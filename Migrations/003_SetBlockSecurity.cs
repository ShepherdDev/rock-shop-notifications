using Rock.Plugin;

namespace com.shepherdchurch.RockShopNotifications.Migrations
{
    [MigrationNumber( 3, "1.6.3" )]
    public class SetBlockSecurity : Migration
    {
        public override void Up()
        {
            //
            // Allow admins to view the block.
            //
            RockMigrationHelper.AddSecurityAuthForBlock( SystemGuid.Block.RECENT_PLUGINS,
                0,
                Rock.Security.Authorization.VIEW,
                true,
                Rock.SystemGuid.Group.GROUP_ADMINISTRATORS,
                Rock.Model.SpecialRole.None,
                "5D9FC590-FAEC-4399-9FBA-0B49D003D003" );

            //
            // Then deny all other users.
            //
            RockMigrationHelper.AddSecurityAuthForBlock( SystemGuid.Block.RECENT_PLUGINS,
                0,
                Rock.Security.Authorization.VIEW,
                false,
                null,
                Rock.Model.SpecialRole.AllUsers,
                "E3FA258E-92DC-4C90-B0CE-62CFDA3DF179" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteSecurityAuth( "E3FA258E-92DC-4C90-B0CE-62CFDA3DF179" );
            RockMigrationHelper.DeleteSecurityAuth( "5D9FC590-FAEC-4399-9FBA-0B49D003D003" );
        }
    }
}
