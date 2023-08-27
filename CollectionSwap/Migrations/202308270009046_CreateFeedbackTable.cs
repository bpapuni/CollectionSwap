namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateFeedbackTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SwapId = c.Int(nullable: false),
                        SenderId = c.String(),
                        ReceiverId = c.String(),
                        Rating = c.Int(nullable: false),
                        PositiveFeedback = c.String(),
                        NeutralFeedback = c.String(),
                        NegativeFeedback = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Swaps", t => t.SwapId, cascadeDelete: true)
                .Index(t => t.SwapId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Feedbacks", "SwapId", "dbo.Swaps");
            DropIndex("dbo.Feedbacks", new[] { "SwapId" });
            DropTable("dbo.Feedbacks");
        }
    }
}
