namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Updates : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Swaps", "CollectionId", "dbo.Collections");
            DropForeignKey("dbo.Swaps", "ReceiverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "ReceiverCollectionId", "dbo.UserCollections");
            DropForeignKey("dbo.Swaps", "SenderId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "SenderCollectionId", "dbo.UserCollections");
            DropIndex("dbo.Swaps", new[] { "CollectionId" });
            DropIndex("dbo.Swaps", new[] { "SenderId" });
            DropIndex("dbo.Swaps", new[] { "SenderCollectionId" });
            DropIndex("dbo.Swaps", new[] { "ReceiverId" });
            DropIndex("dbo.Swaps", new[] { "ReceiverCollectionId" });
            RenameColumn(table: "dbo.Swaps", name: "CollectionId", newName: "Collection_Id");
            RenameColumn(table: "dbo.Swaps", name: "ReceiverId", newName: "Receiver_Id");
            RenameColumn(table: "dbo.Swaps", name: "ReceiverCollectionId", newName: "ReceiverCollection_Id");
            RenameColumn(table: "dbo.Swaps", name: "SenderId", newName: "Sender_Id");
            RenameColumn(table: "dbo.Swaps", name: "SenderCollectionId", newName: "SenderCollection_Id");
            AlterColumn("dbo.Swaps", "Collection_Id", c => c.Int());
            AlterColumn("dbo.Swaps", "Sender_Id", c => c.String(maxLength: 128));
            AlterColumn("dbo.Swaps", "SenderCollection_Id", c => c.Int());
            AlterColumn("dbo.Swaps", "Receiver_Id", c => c.String(maxLength: 128));
            AlterColumn("dbo.Swaps", "ReceiverCollection_Id", c => c.Int());
            CreateIndex("dbo.Swaps", "Collection_Id");
            CreateIndex("dbo.Swaps", "Receiver_Id");
            CreateIndex("dbo.Swaps", "ReceiverCollection_Id");
            CreateIndex("dbo.Swaps", "Sender_Id");
            CreateIndex("dbo.Swaps", "SenderCollection_Id");
            AddForeignKey("dbo.Swaps", "Collection_Id", "dbo.Collections", "Id");
            AddForeignKey("dbo.Swaps", "Receiver_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Swaps", "ReceiverCollection_Id", "dbo.UserCollections", "Id");
            AddForeignKey("dbo.Swaps", "Sender_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Swaps", "SenderCollection_Id", "dbo.UserCollections", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Swaps", "SenderCollection_Id", "dbo.UserCollections");
            DropForeignKey("dbo.Swaps", "Sender_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "ReceiverCollection_Id", "dbo.UserCollections");
            DropForeignKey("dbo.Swaps", "Receiver_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "Collection_Id", "dbo.Collections");
            DropIndex("dbo.Swaps", new[] { "SenderCollection_Id" });
            DropIndex("dbo.Swaps", new[] { "Sender_Id" });
            DropIndex("dbo.Swaps", new[] { "ReceiverCollection_Id" });
            DropIndex("dbo.Swaps", new[] { "Receiver_Id" });
            DropIndex("dbo.Swaps", new[] { "Collection_Id" });
            AlterColumn("dbo.Swaps", "ReceiverCollection_Id", c => c.Int(nullable: false));
            AlterColumn("dbo.Swaps", "Receiver_Id", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.Swaps", "SenderCollection_Id", c => c.Int(nullable: false));
            AlterColumn("dbo.Swaps", "Sender_Id", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.Swaps", "Collection_Id", c => c.Int(nullable: false));
            RenameColumn(table: "dbo.Swaps", name: "SenderCollection_Id", newName: "SenderCollectionId");
            RenameColumn(table: "dbo.Swaps", name: "Sender_Id", newName: "SenderId");
            RenameColumn(table: "dbo.Swaps", name: "ReceiverCollection_Id", newName: "ReceiverCollectionId");
            RenameColumn(table: "dbo.Swaps", name: "Receiver_Id", newName: "ReceiverId");
            RenameColumn(table: "dbo.Swaps", name: "Collection_Id", newName: "CollectionId");
            CreateIndex("dbo.Swaps", "ReceiverCollectionId");
            CreateIndex("dbo.Swaps", "ReceiverId");
            CreateIndex("dbo.Swaps", "SenderCollectionId");
            CreateIndex("dbo.Swaps", "SenderId");
            CreateIndex("dbo.Swaps", "CollectionId");
            AddForeignKey("dbo.Swaps", "SenderCollectionId", "dbo.UserCollections", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Swaps", "SenderId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Swaps", "ReceiverCollectionId", "dbo.UserCollections", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Swaps", "ReceiverId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Swaps", "CollectionId", "dbo.Collections", "Id", cascadeDelete: true);
        }
    }
}
