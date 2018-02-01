using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace com.shepherdchurch.RockShopNotifications.Migrations
{
    [MigrationNumber( 1, "1.6.3" )]
    public class AddSystemData : Migration
    {
        public override void Up()
        {
            //
            // Create Defined Type 'Recent Plugins'.
            //
            RockMigrationHelper.AddDefinedType( "Global", "Recent Plugins",
                "Contains all the plugins that have been detected by the Rock Shop Feed job.",
                SystemGuid.DefinedType.RECENT_PLUGINS );

            RockMigrationHelper.AddDefinedTypeAttribute( SystemGuid.DefinedType.RECENT_PLUGINS,
                Rock.SystemGuid.FieldType.BOOLEAN, "Completed", "Completed",
                "Has the item been acknowledged.",
                0, "False", SystemGuid.Attribute.RECENT_PLUGINS_DT_COMPLETED );
            Sql( string.Format( "UPDATE [Attribute] SET [IsGridColumn] = 1 WHERE [Guid] = '{0}'", SystemGuid.Attribute.RECENT_PLUGINS_DT_COMPLETED ) );

            RockMigrationHelper.AddDefinedTypeAttribute( SystemGuid.DefinedType.RECENT_PLUGINS,
                Rock.SystemGuid.FieldType.TEXT, "Release", "Release",
                "The identifier of the release this defined value is for.",
                1, string.Empty, SystemGuid.Attribute.RECENT_PLUGINS_DT_RELEASE );

            //
            // Create Block 'Recent Plugins'.
            //
            RockMigrationHelper.AddBlock( Rock.SystemGuid.Page.INTERNAL_HOMEPAGE,
                string.Empty, "15572974-dd86-43c8-bbbf-5181ee76e2c9",
                "Recent Plugins", "Main",
                @"<div class=""panel panel-block"">
    <div class=""panel-heading"">
        <h4 class=""panel-title"">Recent Plugins</h4>
    </div>
    <div class=""panel-body"">",
                @"    </div>
</div>",
                0, SystemGuid.Block.RECENT_PLUGINS );

            //
            // Create Job 'Check For New Plugins'.
            //
            Sql( string.Format( @"
    INSERT INTO [ServiceJob] (
         [IsSystem]
        ,[IsActive]
        ,[Name]
        ,[Description]
        ,[Class]
        ,[CronExpression]
        ,[NotificationStatus]
        ,[Guid] )
    VALUES (
         0
        ,1
        ,'Check For New Plugins'
        ,'Performs periodic checks to see if new plugins have been released in the Rock Shop.'
        ,'com.shepherdchurch.RockShopNotifications.Jobs.RockShopFeed'
        ,'0 {0} 22 1/1 * ? *'
        ,1
        ,'{1}'
        )",
            new Random().Next(60), // Make the minute random so we don't overload rockrms.com
            SystemGuid.ServiceJob.CHECK_FOR_NEW_PLUGINS ) );
        }

        public override void Down()
        {
            //
            // Delete Job 'Check For New Plugins'.
            //
            Sql( string.Format( "DELETE FROM [ServiceJob] WHERE [Guid] = '{0}'", SystemGuid.ServiceJob.CHECK_FOR_NEW_PLUGINS ) );

            //
            // Delete Block 'Recent Plugins'.
            //
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.RECENT_PLUGINS );

            //
            // Delete Defined Type 'Recent Plugins'.
            //
            RockMigrationHelper.DeleteDefinedType( SystemGuid.DefinedType.RECENT_PLUGINS );
        }
    }
}
