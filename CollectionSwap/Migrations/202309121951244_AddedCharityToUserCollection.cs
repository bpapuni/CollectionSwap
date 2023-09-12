namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedCharityToUserCollection : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserCollections", "Charity", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserCollections", "Charity");
        }
    }
}
