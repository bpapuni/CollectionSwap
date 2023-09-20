namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateFeedbackUserRelationship : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Feedbacks", "Receiver_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.Feedbacks", "Sender_Id", c => c.String(maxLength: 128));
            CreateIndex("dbo.Feedbacks", "Receiver_Id");
            CreateIndex("dbo.Feedbacks", "Sender_Id");
            AddForeignKey("dbo.Feedbacks", "Receiver_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Feedbacks", "Sender_Id", "dbo.AspNetUsers", "Id");
            DropColumn("dbo.Feedbacks", "SenderId");
            DropColumn("dbo.Feedbacks", "ReceiverId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Feedbacks", "ReceiverId", c => c.String());
            AddColumn("dbo.Feedbacks", "SenderId", c => c.String());
            DropForeignKey("dbo.Feedbacks", "Sender_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Feedbacks", "Receiver_Id", "dbo.AspNetUsers");
            DropIndex("dbo.Feedbacks", new[] { "Sender_Id" });
            DropIndex("dbo.Feedbacks", new[] { "Receiver_Id" });
            DropColumn("dbo.Feedbacks", "Sender_Id");
            DropColumn("dbo.Feedbacks", "Receiver_Id");
        }
    }
}
