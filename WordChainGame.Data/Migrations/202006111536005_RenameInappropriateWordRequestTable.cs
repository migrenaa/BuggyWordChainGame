namespace WordChainGame.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameInappropriateWordRequestTable : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.InappropriateWordRequestMappings", newName: "InappropriateWordRequests");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.InappropriateWordRequests", newName: "InappropriateWordRequestMappings");
        }
    }
}
