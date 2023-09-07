namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCollectionUserCollectionRelationship : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.UserCollections", "CollectionId");
            AddForeignKey("dbo.UserCollections", "CollectionId", "dbo.Collections", "Id", cascadeDelete: true);
            DropColumn("dbo.UserCollections", "Description");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserCollections", "Description", c => c.String());
            DropForeignKey("dbo.UserCollections", "CollectionId", "dbo.Collections");
            DropIndex("dbo.UserCollections", new[] { "CollectionId" });
        }
    }
}
