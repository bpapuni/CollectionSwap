namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateSwapFeedbackRelationship : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "ReceiverFeedback_Id", c => c.Int());
            AddColumn("dbo.Swaps", "SenderFeedback_Id", c => c.Int());
            CreateIndex("dbo.Swaps", "ReceiverFeedback_Id");
            CreateIndex("dbo.Swaps", "SenderFeedback_Id");
            AddForeignKey("dbo.Swaps", "ReceiverFeedback_Id", "dbo.Feedbacks", "Id");
            AddForeignKey("dbo.Swaps", "SenderFeedback_Id", "dbo.Feedbacks", "Id");
            DropColumn("dbo.Swaps", "SenderFeedbackSent");
            DropColumn("dbo.Swaps", "ReceiverFeedbackSent");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Swaps", "ReceiverFeedbackSent", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "SenderFeedbackSent", c => c.Boolean(nullable: false));
            DropForeignKey("dbo.Swaps", "SenderFeedback_Id", "dbo.Feedbacks");
            DropForeignKey("dbo.Swaps", "ReceiverFeedback_Id", "dbo.Feedbacks");
            DropIndex("dbo.Swaps", new[] { "SenderFeedback_Id" });
            DropIndex("dbo.Swaps", new[] { "ReceiverFeedback_Id" });
            DropColumn("dbo.Swaps", "SenderFeedback_Id");
            DropColumn("dbo.Swaps", "ReceiverFeedback_Id");
        }
    }
}
