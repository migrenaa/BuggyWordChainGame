
namespace WordChainGame.Services.Services
{
    using WordChainGame.DTO.Topic;
    using WordChainGame.DTO.Word;

    public interface ITopicService
    {
        DetailsTopicResponseModel Create(TopicRequestModel model, string authorId);
        PaginatedTopicsResponseModel Get(string orderBy, int top, int skip);
        PaginatedWordsResponseModel GetWords(int topicId, int top, int skip);
        ListedWordResponseModel AddWord(int topicId, string userId, WordRequestModel model);
        void RequestWordAsInappropriate(string requesterId, int topicId, int wordId);
    }
}
