namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RefactoredSwapModel : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Swaps", name: "Receiver_Id", newName: "ReceiverId");
            RenameColumn(table: "dbo.Swaps", name: "Sender_Id", newName: "SenderId");
            RenameIndex(table: "dbo.Swaps", name: "IX_Sender_Id", newName: "IX_SenderId");
            RenameIndex(table: "dbo.Swaps", name: "IX_Receiver_Id", newName: "IX_ReceiverId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Swaps", name: "IX_ReceiverId", newName: "IX_Receiver_Id");
            RenameIndex(table: "dbo.Swaps", name: "IX_SenderId", newName: "IX_Sender_Id");
            RenameColumn(table: "dbo.Swaps", name: "SenderId", newName: "Sender_Id");
            RenameColumn(table: "dbo.Swaps", name: "ReceiverId", newName: "Receiver_Id");
        }
    }
}
