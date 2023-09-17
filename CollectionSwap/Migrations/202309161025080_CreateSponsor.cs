namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateSponsor : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Sponsors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CollectionId = c.Int(nullable: false),
                        Statement = c.String(),
                        Url = c.String(),
                        Image = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Sponsors");
        }
    }
}
