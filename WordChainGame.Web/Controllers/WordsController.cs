

namespace WordChainGame.Web.Controllers
{
    using AutoMapper;
    using Swashbuckle.Swagger.Annotations;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web.Http;
    using WordChainGame.Common.CustomExceptions;
    using WordChainGame.DTO.InappropriateWordRequests;
    using WordChainGame.Services.Services.Words;
    using WordChainGame.Services.UnitOfWork;

    [RoutePrefix("api/words")]
    public class WordsController : ApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IWordService words;

        public WordsController(IUnitOfWork unitOfWork, IMapper mapper, IWordService words)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.words = words;
        }

        /// <summary>
        /// Gets all requests for inappropriate word which are still not marked as inappropriate or not.
        /// </summary>
        /// <param name="top">Pagination model</param>
        /// <param name="skip">Pagination model</param>
        /// <returns>Paginated result for the requests</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("inappropriate")]
        [SwaggerResponse(HttpStatusCode.OK, "All inappropriate words requests.", typeof(PaginatedInappropriateWordRequests))]
        public IHttpActionResult Get(int top, int skip)
        {
            var requests = unitOfWork.InappropriateWordRequests
                                     .Get(filter: r => r.IsInappropriate == null,
                                          includeProperties: "Requester, InappropriateWord.Topic");

            var paginatedRequests = requests.Take(top).Skip(skip);
            int count = requests.Count();
            var response = new PaginatedInappropriateWordRequests
            {
                Count = count,
                InappropriateWordRequests = mapper.Map<ICollection<InappropriateWordRequestsResponseModel>>(paginatedRequests),
                NextPageUrl = top + (top - skip) > count ? null : string.Format("api/words/inappropriate?top={0}&skip={1}", top + (top - skip), skip + (top - skip)),
                PreviousPageUrl = skip - (top - skip) < 0 ? null : string.Format("api/words/inappropriate?top={0}&skip={1}", top - (top - skip), skip - (top - skip)) 
            };
            return Ok(response);
        }


        /// <summary>
        /// Removes inappropriate word request.
        /// </summary>
        /// <param name="id">The id of the inappropriate word request</param>
        /// <returns>Details about the request.</returns>
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [Route("{wordId}/inappropriate")]
        [SwaggerResponse(HttpStatusCode.OK, "Suucessfully deleted inappropriate word request.")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Word not found")]
        public IHttpActionResult UpdateInappropriateWord(int wordId)
        {
            var word = unitOfWork.Words.GetByID(wordId);
            if(word == null)
            {
                return NotFound();
            }

            if(word.IsDeleted)
            {
                return NotFound();
            }

            try
            {
                words.DeleteInappropriateWordRequestForWord(wordId);
            } catch (InvalidWordException ex)
            { 
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Deletes word from topic by id and if there is request for inappropriate word, marks the request as completed.
        /// </summary>
        /// <param name="wordId">The id of the word.</param>
        /// <returns>Ok</returns>
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK, "Word successfully deleted.")]
        [SwaggerResponse(HttpStatusCode.NotFound, "Invalid word Id.")]
        public IHttpActionResult Delete(int wordId)
        {
            var word = unitOfWork.Words.GetByID(wordId);
            if (word == null)
            {
                return NotFound();
            }

            if (word.IsDeleted)
            {
                return NotFound();
            }

            words.Delete(wordId);
            return Ok();
        }
    }
}
