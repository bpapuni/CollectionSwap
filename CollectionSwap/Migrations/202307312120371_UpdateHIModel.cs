namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateHIModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HeldItems", "Swap_Id", c => c.Int());
            CreateIndex("dbo.HeldItems", "Swap_Id");
            AddForeignKey("dbo.HeldItems", "Swap_Id", "dbo.Swaps", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HeldItems", "Swap_Id", "dbo.Swaps");
            DropIndex("dbo.HeldItems", new[] { "Swap_Id" });
            DropColumn("dbo.HeldItems", "Swap_Id");
        }
    }
}
