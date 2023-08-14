namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenamedCardSetsToCollections : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Collections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            DropTable("dbo.Collections");
        }
        
        public override void Down()
        {
            //CreateTable(
            //    "dbo.Collections",
            //    c => new
            //        {
            //            Id = c.Int(nullable: false, identity: true),
            //            Name = c.String(nullable: false),
            //        })
            //    .PrimaryKey(t => t.Id);
            
            DropTable("dbo.Collections");
        }
    }
}
