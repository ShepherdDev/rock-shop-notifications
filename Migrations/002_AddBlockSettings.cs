using Rock.Plugin;

namespace com.shepherdchurch.RockShopNotifications.Migrations
{
    [MigrationNumber( 2, "1.6.3" )]
    public class AddBlockSettings : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.RECENT_PLUGINS,
                "9DF52F7A-E122-4205-83DF-F205367DD3D9", "Completed" ); // Attribute Key

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.RECENT_PLUGINS,
                "315BCFA9-AAB5-41D1-BC23-BCB14A6EBC25", SystemGuid.DefinedType.RECENT_PLUGINS ); // Defined Type

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.RECENT_PLUGINS,
                "909F86D7-C3BD-4202-99C0-4C73B0D29AC9", "True" ); // Hide Checked Items

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.RECENT_PLUGINS,
                "42A9404C-835C-469C-AD85-D77573F76C3D", "True" ); // Hide Block When Empty
        }

        public override void Down()
        {
        }
    }
}
