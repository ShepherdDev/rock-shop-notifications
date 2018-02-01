using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using Quartz;
using RestSharp;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Store;

namespace com.shepherdchurch.RockShopNotifications.Jobs
{
    [IntegerField( "Days Back", "Number of days back to consider when looking for new plugins.", false, 30, order: 0 )]
    [BooleanField( "Only Show Updates", "Only show updates to plugins you already have installed on your server.", false, order: 1 )]
    [CodeEditorField( "Checklist Template", "The Lava template to use when building the information about the plugin.", Rock.Web.UI.Controls.CodeEditorMode.Lava, defaultValue: @"
<div class='clearfix'>
    <img src='{{ Release.Package.PackageIconBinaryFile.ImageUrl }}&width=210&height=210&mode=crop&format=jpg&quality=90' class='pull-left margin-r-sm' height='45' style='border-radius: 4px;'>
    <p>
        {{ Release.Package.Vendor.Name }} released {{ Release.VersionLabel }} of the plugin <a href='/page/354?PackageId={{ Release.Package.Id }}'>{{ Release.Package.Name }}</a> on {{ Release.ModifiedDateTime | Date:'MMM d, yyyy' }}.
        {% if InstalledVersion != '' %} <span class='label label-info'>{{ InstalledVersion }} currently installed</span>{% endif %}
        {% if RequiresRockUpdate == true %} <span class='label label-warning'>Requires Rock {{ Release.RequiredRockVersion }}</span>{% endif %}
    </p>
</div>
<p class='margin-t-md'>
    {{ Release.Description }}
</p>
", order: 2 )]
    [DisallowConcurrentExecution]
    public class RockShopFeed : IJob
    {
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            //
            // Get all our configuration options.
            //
            var daysBack = dataMap.GetString( "DaysBack" ).AsIntegerOrNull() ?? 30;
            var onlyShowUpdates = dataMap.GetString( "OnlyShowUpdates" ).AsBoolean();
            var checklistTemplate = dataMap.GetString( "ChecklistTemplate" );

            var rockVersion = Rock.Utility.RockSemanticVersion.Parse( Rock.VersionInfo.VersionInfo.GetRockSemanticVersionNumber() );
            int newReleaseCount = 0;

            //
            // Get currently installed packages.
            //
            var installedPackages = InstalledPackageService.GetInstalledPackages();

            //
            // Get all releases.
            //
            var releases = GetRecentVersions( daysBack )
                .OrderBy( v => v.ModifiedDateTime )
                .ToList();

            //
            // Associate all releases with their package.
            //
            var allPackages = GetAllPackages();
            releases.ForEach( r => r.Package = allPackages.FirstOrDefault( p => p.Id == r.PackageId ) );

            //
            // Generate all the new defined values.
            //
            var definedTypeCache = Rock.Web.Cache.DefinedTypeCache.Read( SystemGuid.DefinedType.RECENT_PLUGINS.AsGuid() );
            using ( var rockContext = new RockContext() )
            {
                var definedValueService = new Rock.Model.DefinedValueService( rockContext );
                int nextOrder = definedTypeCache.DefinedValues.Select( dv => dv.Order ).DefaultIfEmpty().Max() + 1;

                foreach ( var release in releases )
                {
                    //
                    // If we have already posted this release to the checklist then skip it.
                    //
                    if ( definedTypeCache.DefinedValues.Any( dv => dv.GetAttributeValue( "Release" ).AsInteger() == release.Id ) )
                    {
                        continue;
                    }

                    var installedVersion = installedPackages
                        .Where( p => p.PackageId == release.PackageId )
                        .OrderByDescending( p => p.VersionId )
                        .Select( p => p.VersionLabel )
                        .FirstOrDefault();

                    //
                    // If they only want to see updates, ignore anything where we don't have a version already
                    // installed.
                    //
                    if ( onlyShowUpdates && string.IsNullOrWhiteSpace( installedVersion ) )
                    {
                        continue;
                    }

                    var mergeFields = new Dictionary<string, object>
                    {
                        { "Release", release },
                        { "InstalledVersion", installedVersion ?? string.Empty },
                        { "RequiresRockUpdate", release.RequiredRockSemanticVersion > rockVersion }
                    };

                    var definedValue = new Rock.Model.DefinedValue
                    {
                        DefinedTypeId = definedTypeCache.Id,
                        Value = release.Package.Name + " " + release.VersionLabel,
                        Description = checklistTemplate.ResolveMergeFields( mergeFields ),
                        Order = nextOrder++
                    };
                    definedValueService.Add( definedValue );

                    definedValue.LoadAttributes( rockContext );
                    definedValue.SetAttributeValue( "Release", release.Id );

                    rockContext.SaveChanges();

                    definedValue.SaveAttributeValues( rockContext );

                    newReleaseCount += 1;
                }
            }

            Rock.Web.Cache.DefinedTypeCache.Flush( definedTypeCache.Id );

            context.Result = string.Format( "Found {0} new {1}.", newReleaseCount, "release".PluralizeIf( newReleaseCount != 1 ) );
        }

        /// <summary>
        /// Gets all packages.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Package> GetAllPackages()
        {
            var client = new RestClient( ConfigurationManager.AppSettings["RockStoreUrl"] )
            {
                Timeout = 15000
            };
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "Api/Packages"
            };
            request.AddParameter( "$expand", "Vendor,PackageIconBinaryFile" );

            var response = client.Execute<List<Package>>( request );

            if ( response.ResponseStatus == ResponseStatus.Completed )
            {
                return response.Data;
            }
            else
            {
                return new List<Package>();
            }
        }

        /// <summary>
        /// Gets all packages.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PackageVersion> GetRecentVersions( int daysBack )
        {
            var limitDate = DateTime.Now.AddDays( -daysBack );
            var client = new RestClient( ConfigurationManager.AppSettings["RockStoreUrl"] )
            {
                Timeout = 15000
            };
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "Api/PackageVersions"
            };
            request.AddParameter( "$filter", string.Format( "ModifiedDateTime gt DateTime'{0}'", limitDate.ToString( "yyyy-MM-dd" ) ) );

            var response = client.Execute<List<PackageVersion>>( request );

            if ( response.ResponseStatus == ResponseStatus.Completed )
            {
                //
                // Filter client-side by Status == 2 (released) and also filter out
                // any versions that were created more than 90 days before they were
                // last modified. This helps remove items that were modified by the Spark
                // team after release.
                //
                return response.Data
                    .Where( v => v.Status == 2 )
                    .Where( v => v.ModifiedDateTime.Subtract(v.CreatedDateTime).TotalDays < 90 )
                    .ToList();
            }
            else
            {
                return new List<PackageVersion>();
            }
        }

        #region Support Classes

        /// <summary>
        /// Extends the original PackageVersion class to include extra fields the server sends
        /// us but are not included in the original.
        /// </summary>
        public class PackageVersion : Rock.Store.PackageVersion
        {
            public Package Package { get; set; }

            public int PackageId { get; set; }

            public DateTime ModifiedDateTime { get; set; }

            public DateTime CreatedDateTime { get; set; }

            public int Status { get; set; }
        }

        #endregion
    }
}
