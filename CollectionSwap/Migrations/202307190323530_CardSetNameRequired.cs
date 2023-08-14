namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CardSetNameRequired : DbMigration
    {
        public override void Up()
        {
            //AlterColumn("dbo.CardSets", "card_set_name", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            //AlterColumn("dbo.CardSets", "card_set_name", c => c.String());
        }
    }
}
