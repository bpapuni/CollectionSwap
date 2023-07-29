namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateSwapsTable1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderUserCollectionId", c => c.Int(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverUserCollectionId", c => c.Int(nullable: false));
            AddColumn("dbo.Swaps", "SenderId", c => c.String(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "ReceiverId");
            DropColumn("dbo.Swaps", "SenderId");
            DropColumn("dbo.Swaps", "ReceiverUserCollectionId");
            DropColumn("dbo.Swaps", "SenderUserCollectionId");
        }
    }
}
