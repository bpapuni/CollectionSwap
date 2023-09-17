namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateCollectionSponsorRelationship : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Collections", "Sponsor_Id", c => c.Int());
            CreateIndex("dbo.Collections", "Sponsor_Id");
            AddForeignKey("dbo.Collections", "Sponsor_Id", "dbo.Sponsors", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Collections", "Sponsor_Id", "dbo.Sponsors");
            DropIndex("dbo.Collections", new[] { "Sponsor_Id" });
            DropColumn("dbo.Collections", "Sponsor_Id");
        }
    }
}
