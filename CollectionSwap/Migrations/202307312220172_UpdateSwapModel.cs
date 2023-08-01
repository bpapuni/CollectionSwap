namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateSwapModel : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Swaps", "CollectionId");
            AddForeignKey("dbo.Swaps", "CollectionId", "dbo.Collections", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Swaps", "CollectionId", "dbo.Collections");
            DropIndex("dbo.Swaps", new[] { "CollectionId" });
        }
    }
}
