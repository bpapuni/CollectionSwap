namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixedSwapReceivedTypo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderConfirmReceived", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverConfirmReceived", c => c.Boolean(nullable: false));
            DropColumn("dbo.Swaps", "SenderConfirmReceieved");
            DropColumn("dbo.Swaps", "ReceiverConfirmReceieved");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Swaps", "ReceiverConfirmReceieved", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "SenderConfirmReceieved", c => c.Boolean(nullable: false));
            DropColumn("dbo.Swaps", "ReceiverConfirmReceived");
            DropColumn("dbo.Swaps", "SenderConfirmReceived");
        }
    }
}
