using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.UI.WebControls.WebParts;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    public class OutboundOrderController : ApiController
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IStockRepository stockRepository;
        private readonly IProductRepository productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            this.stockRepository = stockRepository;
            this.productRepository = productRepository;
        }

        public void Post([FromBody]OutboundOrderRequestModel request)
        {
            //END POINT
            log.Info(String.Format("Processing outbound order: {0}", request));

            //it gets the gtins from the requests orderlines into VAR GTINS and checks they are not duplicated
            var gtins = new List<String>();
            foreach (var orderLine in request.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(String.Format("Outbound order request contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            //IT Gets ALL the information for the products we have into our var gtins from the DB and set IT up into a DICTIONARY
            var productDataModels = productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            var lineItems = new List<StockAlteration>();
            var productGtins = new List<string>();
            var errors = new List<string>();

            foreach (var orderLine in request.OrderLines)
            {
                //Throws an error if the dictionary does not content a product listing in the OrderLine
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(string.Format("Unknown product gtin: {0}", orderLine.gtin));
                }
                else
                {
                    var product = products[orderLine.gtin]; // Selects a product from the dictionary by its gtin
                    lineItems.Add(new StockAlteration(product.Gtin, orderLine.quantity));
                    productGtins.Add(product.Gtin);
                }
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productGtins);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.Gtin))
                {
                    errors.Add(string.Format("Product: {0}, no stock held", orderLine.gtin));
                    continue;
                }

                var item = stock[lineItem.Gtin];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        string.Format("Product: {0}, stock held: {1}, stock to remove: {2}", orderLine.gtin, item.held,
                            lineItem.Quantity));
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            stockRepository.RemoveStock(request.WarehouseId, lineItems);
        }
    }
}