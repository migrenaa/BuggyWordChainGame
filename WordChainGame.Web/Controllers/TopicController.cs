
namespace WordChainGame.Web.Controllers
{
    using AutoMapper;
    using Microsoft.AspNet.Identity;
    using Swashbuckle.Swagger.Annotations;
    using System.Net;
    using System.Web.Http;
    using WordChainGame.Common.CustomExceptions;
    using WordChainGame.Data.Entities;
    using WordChainGame.DTO.Topic;
    using WordChainGame.DTO.Word;
    using WordChainGame.Services.Services;
    using WordChainGame.Services.UnitOfWork;

    [RoutePrefix("api/topics")]
    public class TopicController : ApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ITopicService topics;

        public TopicController(IUnitOfWork unitOfWork, IMapper mapper, ITopicService topics)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.topics = topics;
        }


        /// <summary>
        /// Gets all topics ordered. The result is paginated.
        /// </summary>
        /// <param name="orderBy">name/wordscount</param>
        /// <param name="top">Pagination model</param>
        /// <param name="skip">Pagination model</param>
        /// <returns>Information about the topics and the author</returns>
        [HttpGet]
        [Authorize(Roles = "User")]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK, "Orders the result by given property.", typeof(PaginatedTopicsResponseModel))]

        public IHttpActionResult Get(string orderBy, int top = 10, int skip = 0)
        {
            top = top < 0 ? 10 : top;
            skip = skip < 0 ? 0 : skip;
            return Ok(this.topics.Get(orderBy, top, skip));
        }


        /// <summary>
        /// Creates new topic.
        /// </summary>
        /// <param name="model">The information about the topic</param>
        /// <returns>Details about inserted topic</returns>
        [Authorize(Roles = "User")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request model")]
        [SwaggerResponse(HttpStatusCode.OK, "Topic successfully created.", typeof(DetailsTopicResponseModel))]
        public IHttpActionResult Post(TopicRequestModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
            var added = topics.Create(model, User.Identity.GetUserId());
            return Ok(added);

            } catch (InvalidTopicException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Gets all words in a topic.
        /// </summary>
        /// <returns>Details about inserted topic</returns>
        [Authorize(Roles = "User")]
        [HttpGet]
        [Route("{topicId}/words")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Invalid topic id.")]
        [SwaggerResponse(HttpStatusCode.OK, "All words by topic paginated, ordered by date created.", typeof(PaginatedWordsResponseModel))]
        public IHttpActionResult GetWords(int topicId, int top = 0, int skip = 0)
        {
            if (this.unitOfWork.Topics.GetByID(topicId) == null)
            {
                return NotFound();
            }

            top = top < 0 ? 10 : top;
            skip = skip < 0 ? 0 : skip;

            return Ok(this.topics.GetWords(topicId, top, skip));
        }

        /// <summary>
        /// Inserts word in a topic.
        /// </summary>
        /// <returns>Details about inserted topic</returns>
        [Authorize(Roles = "User")]
        [HttpPost]
        [Route("{topicId}/words")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Invalid topic id.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "The word should start with other character or it is already added to this topic.")]
        [SwaggerResponse(HttpStatusCode.OK, "Word successfully inserted.", typeof(DetailsWordResponseModel))]
        public IHttpActionResult AddWord(int topicId, WordRequestModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            string authorId = User.Identity.GetUserId();
            try
            {
                return Ok(this.topics.AddWord(topicId, authorId, model));
            }
            catch (InvalidWordException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Requests inappropriate word in a topic.
        /// </summary>
        /// <param name="topicId">The id of the topic</param>
        /// <param name="wordId">The id of the word.</param>
        /// <returns>Details about the word.</returns>
        [Authorize(Roles = "User")]
        [HttpPost]
        [Route("{topicId}/words/{wordId}/inappropriate")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Invalid topic id.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid word id.")]
        [SwaggerResponse(HttpStatusCode.OK, "Word successfully inserted.")]
        public IHttpActionResult AddInapropriateWord(int topicId, int wordId)
        {
            if (this.unitOfWork.Topics.GetByID(topicId) == null)
            {
                return NotFound();
            }

            if (this.unitOfWork.Words.GetByID(wordId) == null)
            {
                return BadRequest("Invalid word id. ");
            }

            string authorId = User.Identity.GetUserId();

            try
            {
            this.topics.RequestWordAsInappropriate(authorId, topicId, wordId);
            } catch (InvalidWordException ex)
            {
                return  BadRequest(ex.Message);
            }
            return Ok();
        }
    }
}