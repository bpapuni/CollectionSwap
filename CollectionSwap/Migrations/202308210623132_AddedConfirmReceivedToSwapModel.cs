namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedConfirmReceivedToSwapModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderConfirmReceieved", c => c.Boolean(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverConfirmReceieved", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "ReceiverConfirmReceieved");
            DropColumn("dbo.Swaps", "SenderConfirmReceieved");
        }
    }
}
