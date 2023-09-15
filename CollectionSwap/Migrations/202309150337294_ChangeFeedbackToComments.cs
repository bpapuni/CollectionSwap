namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeFeedbackToComments : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Feedbacks", "Comments", c => c.String());
            DropColumn("dbo.Feedbacks", "PositiveFeedback");
            DropColumn("dbo.Feedbacks", "NeutralFeedback");
            DropColumn("dbo.Feedbacks", "NegativeFeedback");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Feedbacks", "NegativeFeedback", c => c.String());
            AddColumn("dbo.Feedbacks", "NeutralFeedback", c => c.String());
            AddColumn("dbo.Feedbacks", "PositiveFeedback", c => c.String());
            DropColumn("dbo.Feedbacks", "Comments");
        }
    }
}
