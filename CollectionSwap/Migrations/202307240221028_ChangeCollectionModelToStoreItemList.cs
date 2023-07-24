namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeCollectionModelToStoreItemList : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "ItemListJSON", c => c.String());
            AddColumn("dbo.UserCollections", "ItemCountJSON", c => c.String());
            DropColumn("dbo.Collections", "Size");
            DropColumn("dbo.UserCollections", "ItemIdsJSON");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserCollections", "ItemIdsJSON", c => c.String());
            AddColumn("dbo.Collections", "Size", c => c.Int(nullable: false));
            DropColumn("dbo.UserCollections", "ItemCountJSON");
            DropColumn("dbo.Collections", "ItemListJSON");
        }
    }
}
