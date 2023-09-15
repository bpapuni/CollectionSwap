namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAddressToUserModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Address_Id", c => c.Int());
            CreateIndex("dbo.AspNetUsers", "Address_Id");
            AddForeignKey("dbo.AspNetUsers", "Address_Id", "dbo.Addresses", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUsers", "Address_Id", "dbo.Addresses");
            DropIndex("dbo.AspNetUsers", new[] { "Address_Id" });
            DropColumn("dbo.AspNetUsers", "Address_Id");
        }
    }
}
