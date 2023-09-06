namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedSwapSizeToModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SwapSize", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "SwapSize");
        }
    }
}
