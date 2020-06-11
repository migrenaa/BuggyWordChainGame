namespace WordChainGame.Services.Services
{
    using AutoMapper;
    using Microsoft.AspNet.Identity;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WordChainGame.Common.CustomExceptions;
    using WordChainGame.Data.Entities;
    using WordChainGame.DTO.Topic;
    using WordChainGame.DTO.Word;
    using WordChainGame.Services.UnitOfWork;


    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public TopicService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        public DetailsTopicResponseModel Create(TopicRequestModel model, string authorId)
        {
            var topic = mapper.Map<Topic>(model);
            topic.AuthorId = authorId;
            topic.WordsCount = 0; 
            foreach(var oldTopic in unitOfWork.Topics.Get().ToList())
            {
                if(oldTopic.Name == topic.Name)
                {
                    throw new InvalidTopicException(string.Format("$The topic {0} already exists.", topic.Name));
                }
            }

            var added = unitOfWork.Topics.Insert(topic);

            unitOfWork.Commit();

            return  mapper.Map<DetailsTopicResponseModel>(added);
        }
        public PaginatedTopicsResponseModel Get(string orderBy, int top, int skip)
        {
            var topics = new List<Topic>();
            if (orderBy.ToLower() == "name")
            {
                topics = unitOfWork.Topics.Get(orderBy: t => t.OrderBy(s => s.Name), includeProperties: "Author").ToList();
            }
            else if (orderBy.ToLower() == "wordscount")
            {
                topics = unitOfWork.Topics.Get(orderBy: t => t.OrderBy(s => s.WordsCount), includeProperties: "Author").ToList();
            }
            else
            {
                topics = unitOfWork.Topics.Get(includeProperties: "Author").ToList();
            }

            var count = topics.Count;
            var paginatedTopics = topics.Take(top).Skip(skip);
            var response = new PaginatedTopicsResponseModel
            {
                Topics = this.mapper.Map<ICollection<ListedTopicResponseModel>>(paginatedTopics),
                Count = count, 
                NextPageUrl = top + (top - skip) > count ? null : string.Format("api/topics?orderby={0}&top={1}&skip={2}", orderBy, top + (top - skip), skip + (top - skip)),
                PreviousPageUrl = skip - (top - skip) < 0 ? null : string.Format("api/topics?orderby={0}&top={1}&skip={2}", orderBy, top - (top - skip), skip - (top - skip)),

            };
            return response;
        }

        public PaginatedWordsResponseModel GetWords(int topicId, int top, int skip)
        {
            var words = unitOfWork.Words.Get(x => x.TopicId == topicId && !x.IsDeleted, includeProperties: "Topic.Author");
            var count = words.Count();
            var paginatedWords = words.Take(top).Skip(skip); 
            var response = new PaginatedWordsResponseModel
            {
                Words = mapper.Map<ICollection<ListedWordResponseModel>>(paginatedWords),
                Count = words.Count(),
                NextPageUrl = top + (top - skip) > count ? null : string.Format("api/topics/{0}/words&top={1}&skip={2}", topicId, top + (top - skip), skip + (top - skip)),
                PreviousPageUrl = skip - (top - skip) < 0 ? null : string.Format("api/topics/{0}/words&top={1}&skip={2}", topicId, top - (top - skip), skip - (top - skip))
            };
            return response;
        }

        public ListedWordResponseModel AddWord(int topicId, string userId, WordRequestModel model)
        {
            var topic = unitOfWork.Topics
                                  .Get(filter: t => t.Id == topicId,
                                       includeProperties: "Words, Author")
                                  .SingleOrDefault();

            var lastWord = topic.Words.OrderBy(w => w.DateCreated)
                                      .LastOrDefault();

            if(lastWord != null)
            {
                var lastWordLastCharachter = lastWord.WordContent.Last().ToString().ToLower();

                if (topic.Words.Where(w => !w.IsDeleted).Select(w => w.WordContent).Contains(model.Word))
                {
                    throw new InvalidWordException($"The word is already added in this topic.");
                }


                if (topic.Words.Where(w => w.IsDeleted).Select(w => w.WordContent).Contains(model.Word))
                {
                    throw new InvalidWordException($"The word is already marked as inappropriate in this topic.");
                }

                if (!model.Word.ToLower().StartsWith(lastWordLastCharachter))
                {
                    throw new InvalidWordException($"The new word should start with {lastWordLastCharachter.ToUpper()}.");
                }
            }
           
            var word = mapper.Map<Word>(model);
            word.AuthorId = userId;
            word.DateCreated = DateTime.Now;
            word.TopicId = topicId;

            unitOfWork.Words.Insert(word);
            topic.WordsCount++;

            unitOfWork.Commit(); 

            return mapper.Map<ListedWordResponseModel>(word);

        }

        public void RequestWordAsInappropriate(string requesterId, int topicId, int wordId)
        {
            if (unitOfWork.Words.GetByID(wordId).AuthorId == requesterId)
            {
                throw new InvalidWordException($"Users cannot make inappropriate word requests for their own words.");
            }

            var inappropriateWordRequest = new InappropriateWordRequest
            {
                DateCreated = DateTime.Now,
                InappropriateWordId = wordId,
                RequesterId = requesterId
            };

            unitOfWork.InappropriateWordRequests.Insert(inappropriateWordRequest);
            unitOfWork.Commit();
        }
    }
}
