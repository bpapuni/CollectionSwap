namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SwapViewModelOverhaul : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderCollectionId", c => c.Int(nullable: false));
            AddColumn("dbo.Swaps", "SenderRequestedItems", c => c.String(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverCollectionId", c => c.Int(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverRequestedItems", c => c.String(nullable: false));
            CreateIndex("dbo.Swaps", "SenderCollectionId");
            CreateIndex("dbo.Swaps", "ReceiverCollectionId");
            AddForeignKey("dbo.Swaps", "ReceiverCollectionId", "dbo.UserCollections", "Id", cascadeDelete: false);
            AddForeignKey("dbo.Swaps", "SenderCollectionId", "dbo.UserCollections", "Id", cascadeDelete: false);
            DropColumn("dbo.Swaps", "SenderUserCollectionId");
            DropColumn("dbo.Swaps", "ReceiverUserCollectionId");
            DropColumn("dbo.Swaps", "SenderItemIdsJSON");
            DropColumn("dbo.Swaps", "ReceiverItemIdsJSON");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Swaps", "ReceiverItemIdsJSON", c => c.String(nullable: false));
            AddColumn("dbo.Swaps", "SenderItemIdsJSON", c => c.String(nullable: false));
            AddColumn("dbo.Swaps", "ReceiverUserCollectionId", c => c.Int(nullable: false));
            AddColumn("dbo.Swaps", "SenderUserCollectionId", c => c.Int(nullable: false));
            DropForeignKey("dbo.Swaps", "SenderCollectionId", "dbo.UserCollections");
            DropForeignKey("dbo.Swaps", "ReceiverCollectionId", "dbo.UserCollections");
            DropIndex("dbo.Swaps", new[] { "ReceiverCollectionId" });
            DropIndex("dbo.Swaps", new[] { "SenderCollectionId" });
            DropColumn("dbo.Swaps", "ReceiverRequestedItems");
            DropColumn("dbo.Swaps", "ReceiverCollectionId");
            DropColumn("dbo.Swaps", "SenderRequestedItems");
            DropColumn("dbo.Swaps", "SenderCollectionId");
        }
    }
}
