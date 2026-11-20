namespace Snake.Application.ViewModel
{
    public static class UserTestJson
    {
        public static readonly string One = @"
        {                            
            ""id"": ""1"",
            ""name"": ""Dragon"",
            ""createdWhen"": ""2021-12-02"",
            ""creatorId"": ""123"",
            ""creatorName"": ""Ivanov Ivan"",
            ""distinguishedName"": ""1.2.3""
        }";

        public static readonly string List = @"
        { 
            ""items"": 
                        [
                            {
                               ""id"": ""1"",
                                ""name"": ""Dragon"",
                                ""createdWhen"": ""2021-12-02"",
                                ""creatorId"": ""123"",
                                ""creatorName"": ""Ivanov Ivan"",
                                ""distinguishedName"": ""1.2.3""
                            }, 
                            {
                                ""id"": ""2"",
                                ""name"": ""Wolf"",
                                ""createdWhen"": ""2021-11-01"",
                                ""creatorId"": ""456"",
                                ""creatorName"": ""Petrov Pert"",
                                ""distinguishedName"": ""4.5.6""
                            },
                            {
                                ""id"": ""3"",
                                ""name"": ""Bear"",
                                ""createdWhen"": ""2021-10-03"",
                                ""creatorId"": ""789"",
                                ""creatorName"": ""Sidorov Sidr"",
                                ""distinguishedName"": ""7.8.9""
                            }
                        ] 
        }";
    }
}
