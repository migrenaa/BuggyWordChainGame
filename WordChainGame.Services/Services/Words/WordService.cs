

namespace WordChainGame.Services.Services.Words
{
    using System.Linq;
    using WordChainGame.Common.CustomExceptions;
    using WordChainGame.Services.UnitOfWork;
    public class WordService : IWordService
    {
        private readonly IUnitOfWork unitOfWork;

        public WordService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }


        public void Delete(int wordId)
        {
            var inappropriateRequest = unitOfWork.InappropriateWordRequests
                                                 .Get(filter: r => r.InappropriateWordId == wordId,
                                                      includeProperties: "InappropriateWord.Topic")
                                                 .ToList();
            
            for (int i = 0; i < inappropriateRequest.Count(); i++)
            {
                inappropriateRequest[i].IsInappropriate = true;
            }

            inappropriateRequest.FirstOrDefault().InappropriateWord.Topic.WordsCount--;
            inappropriateRequest.FirstOrDefault().InappropriateWord.IsDeleted = true;
            unitOfWork.Commit();
        }

        public void DeleteInappropriateWordRequestForWord(int wordId)
        {
            var inappropriateRequest = unitOfWork.InappropriateWordRequests
                                                 .Get(filter: r => r.InappropriateWordId == wordId,
                                                      includeProperties: "InappropriateWord.Topic")
                                                 .ToList();

            if (inappropriateRequest.Count == 0)
            {
                throw new InvalidWordException($"There are no inappropriate word requests for this word.");
            }

            for (int i = 0; i < inappropriateRequest.Count(); i++)
            {
                inappropriateRequest[i].IsInappropriate = false;
            }

            unitOfWork.Commit();
        }
    }
}
