namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedSwapFeedbackRelationship : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Feedbacks", "SwapId", "dbo.Swaps");
            DropIndex("dbo.Feedbacks", new[] { "SwapId" });
        }
        
        public override void Down()
        {
            CreateIndex("dbo.Feedbacks", "SwapId");
            AddForeignKey("dbo.Feedbacks", "SwapId", "dbo.Swaps", "Id", cascadeDelete: true);
        }
    }
}
