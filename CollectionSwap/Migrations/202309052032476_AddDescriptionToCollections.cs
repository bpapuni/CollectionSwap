namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDescriptionToCollections : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "Description", c => c.String());
            AddColumn("dbo.UserCollections", "Description", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserCollections", "Description");
            DropColumn("dbo.Collections", "Description");
        }
    }
}
