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

            // Insert initial data
            Sql("INSERT INTO dbo.Collections (Name, ItemListJSON, Description) VALUES ('Feelings', '[\"1.png\",\"2.png\",\"3.png\",\"4.png\",\"5.png\",\"6.png\",\"7.png\",\"8.png\",\"9.png\",\"10.png\",\"11.png\",\"12.png\",\"13.png\",\"14.png\",\"15.png\"]', 'Explore the richness of human feelings')");
            Sql("INSERT INTO dbo.Collections (Name, ItemListJSON, Description) VALUES ('Women of History', '[\"1.png\",\"2.png\",\"3.png\",\"4.png\",\"5.png\",\"6.png\",\"7.png\",\"8.png\",\"9.png\",\"10.png\",\"11.png\",\"12.png\",\"13.png\",\"14.png\",\"15.png\",\"16.png\"]', 'Celebrate women who shaped history')");
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserCollections", "Description");
            DropColumn("dbo.Collections", "Description");
        }
    }
}
