namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddArchiveBoolToUserCollection : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserCollections", "Archived", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserCollections", "Archived");
        }
    }
}
