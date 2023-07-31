namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateHeldItems : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HeldItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ItemListJSON = c.String(),
                        UserCollection_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UserCollections", t => t.UserCollection_Id)
                .Index(t => t.UserCollection_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HeldItems", "UserCollection_Id", "dbo.UserCollections");
            DropIndex("dbo.HeldItems", new[] { "UserCollection_Id" });
            DropTable("dbo.HeldItems");
        }
    }
}
