namespace UniBridge
{
    public class PurchaseResult
    {
        public PurchaseStatus Status { get; set; }
        public string ProductId { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsSuccess => Status == PurchaseStatus.Success;
        public bool IsRestore => Status == PurchaseStatus.Restored;

        public static PurchaseResult FromSuccess(string productId, string transactionId = null)
        {
            return new PurchaseResult
            {
                Status = PurchaseStatus.Success,
                ProductId = productId,
                TransactionId = transactionId
            };
        }

        public static PurchaseResult FromRestored(string productId, string transactionId = null)
        {
            return new PurchaseResult
            {
                Status = PurchaseStatus.Restored,
                ProductId = productId,
                TransactionId = transactionId
            };
        }

        public static PurchaseResult FromFailed(string productId, PurchaseStatus status, string error = null)
        {
            return new PurchaseResult
            {
                Status = status,
                ProductId = productId,
                ErrorMessage = error
            };
        }
    }
}
