namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateUCModel : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.UserCollections", name: "User_Id", newName: "UserId");
            RenameIndex(table: "dbo.UserCollections", name: "IX_User_Id", newName: "IX_UserId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.UserCollections", name: "IX_UserId", newName: "IX_User_Id");
            RenameColumn(table: "dbo.UserCollections", name: "UserId", newName: "User_Id");
        }
    }
}
