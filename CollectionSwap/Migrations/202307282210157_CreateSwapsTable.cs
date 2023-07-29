namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateSwapsTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Swaps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CollectionId = c.Int(nullable: false),
                        SenderItemIdsJSON = c.String(nullable: false),
                        RecieverItemIdsJSON = c.String(nullable: false),
                        Status = c.String(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: true),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Swaps");
        }
    }
}
