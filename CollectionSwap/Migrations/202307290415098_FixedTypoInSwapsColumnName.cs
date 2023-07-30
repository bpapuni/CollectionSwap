namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixedTypoInSwapsColumnName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "ReceiverItemIdsJSON", c => c.String(nullable: false));
            DropColumn("dbo.Swaps", "RecieverItemIdsJSON");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Swaps", "RecieverItemIdsJSON", c => c.String(nullable: false));
            DropColumn("dbo.Swaps", "ReceiverItemIdsJSON");
        }
    }
}
