namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RefactoredModels : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserCollections", "User_Id", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.Swaps", "Receiver_Id", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.Swaps", "Sender_Id", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.UserCollections", "User_Id");
            CreateIndex("dbo.Swaps", "Receiver_Id");
            CreateIndex("dbo.Swaps", "Sender_Id");
            AddForeignKey("dbo.UserCollections", "User_Id", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Swaps", "Receiver_Id", "dbo.AspNetUsers", "Id", cascadeDelete: false);
            AddForeignKey("dbo.Swaps", "Sender_Id", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            DropColumn("dbo.UserCollections", "UserId");
            DropColumn("dbo.Swaps", "SenderId");
            DropColumn("dbo.Swaps", "ReceiverId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Swaps", "ReceiverId", c => c.String(nullable: false));
            AddColumn("dbo.Swaps", "SenderId", c => c.String(nullable: false));
            AddColumn("dbo.UserCollections", "UserId", c => c.String(nullable: false));
            DropForeignKey("dbo.Swaps", "Sender_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "Receiver_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.UserCollections", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.Swaps", new[] { "Sender_Id" });
            DropIndex("dbo.Swaps", new[] { "Receiver_Id" });
            DropIndex("dbo.UserCollections", new[] { "User_Id" });
            DropColumn("dbo.Swaps", "Sender_Id");
            DropColumn("dbo.Swaps", "Receiver_Id");
            DropColumn("dbo.UserCollections", "User_Id");
        }
    }
}
