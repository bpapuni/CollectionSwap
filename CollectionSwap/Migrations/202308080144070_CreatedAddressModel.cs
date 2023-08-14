namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreatedAddressModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Addresses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false),
                        FullName = c.String(nullable: false),
                        CompanyName = c.String(),
                        LineOne = c.String(nullable: false),
                        LineTwo = c.String(nullable: true),
                        PostCode = c.String(nullable: false),
                        City = c.String(nullable: false),
                        Created = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Addresses");
        }
    }
}
