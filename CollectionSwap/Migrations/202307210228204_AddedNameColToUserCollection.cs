namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedNameColToUserCollection : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserCollections", "Name", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserCollections", "Name");
        }
    }
}
