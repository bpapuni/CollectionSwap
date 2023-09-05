namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SwapsTableTrackFeedbackSent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderFeedbackSent", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverFeedbackSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "ReceiverFeedbackSent");
            DropColumn("dbo.Swaps", "SenderFeedbackSent");
        }
    }
}
