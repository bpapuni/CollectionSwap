namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConfirmSentAddedToSwapModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderConfirmSent", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverConfirmSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "ReceiverConfirmSent");
            DropColumn("dbo.Swaps", "SenderConfirmSent");
        }
    }
}
