namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateCollectionCreateUserCollection : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserCollections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        CollectionId = c.Int(nullable: false),
                        ItemIdsJSON = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            //AddColumn("dbo.Collections", "Size", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            //DropColumn("dbo.Collections", "Size");
            DropTable("dbo.UserCollections");
        }
    }
}
