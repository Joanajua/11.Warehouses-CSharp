using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IStockRepository
    {
        int GetTrackedItemsCount();
        int GetStockHeldSum();
        IEnumerable<InboundDataModel> GetInboundByWarehouseId(int id);
        Dictionary<string, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<string> productGtins);
        void RemoveStock(int warehouseId, List<StockAlteration> lineItems);
        void AddStock(int warehouseId, List<StockAlteration> lineItems);
    }

    public class StockRepository : RepositoryBase, IStockRepository
    {

        public int GetTrackedItemsCount()
        {
            string sql = "SELECT COUNT(*) FROM stock";
            return (int)QueryForLong(sql);
        }

        public int GetStockHeldSum()
        {
            string sql = "SELECT SUM(hld) FROM stock";
            return (int)QueryForLong(sql);
        }

        public IEnumerable<InboundDataModel> GetInboundByWarehouseId(int id)
        {
            string sql = "SELECT * FROM inbound WHERE w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", id);
            string noProductWithIdErrorMessage = string.Format("No stock found with w_id: {0}", id);
            try
            {
                return base.RunGetQuery(sql, reader => new InboundDataModel(reader), noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<InboundDataModel>();
            }
        }

        public Dictionary<string, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<string> productGtins)
        {
            string sql = string.Format("SELECT w_id, hld, gtin_cd FROM stock WHERE w_id = @w_id AND gtin_cd IN ('{0}')",
                String.Join("','", productGtins));
            var parameter = new NpgsqlParameter("@w_id", warehouseId);
            string noProductWithIdErrorMessage = string.Format("No stock found with w_id: {0} and gtin: {1}",
                warehouseId, String.Join(",", productGtins));
            var stock = base.RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter);
            return stock.ToDictionary(s => s.ProductGtin, s => s);
        }
            
        public void AddStock(int warehouseId, List<StockAlteration> lineItems)
        {
            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var orderLine in lineItems)
            {
                parametersList.Add(
                    new NpgsqlParameter[] {
                        new NpgsqlParameter("@gtin_cd", orderLine.Gtin),
                        new NpgsqlParameter("@w_id", warehouseId),
                        new NpgsqlParameter("@hld", orderLine.Quantity)
                    });
            }

            string sql = "INSERT INTO stock (w_id, hld, gtin_cd) VALUES (@w_id, @hld, @gtin_cd) "
                         + "ON CONFLICT (w_id, gtin_cd) DO UPDATE SET hld = stock.hld + EXCLUDED.hld";

            var recordsAffected = new List<int>();
            foreach (var parameters in parametersList)
            {
                 recordsAffected.Add(
                     RunSingleQueryAndReturnRecordsAffected(sql, parameters)
                 );
            }

            string errorMessage = null;

            for (int i = 0; i < recordsAffected.Count; i++)
            {
                if (recordsAffected[i] == 0)
                {
                    errorMessage = String.Format("Product {0} in warehouse {1} was unexpectedly not updated (rows updated returned {2})",
                        parametersList[i][0], warehouseId, recordsAffected[i]);
                }
            }

            if (errorMessage != null)
            {
                throw new InvalidStateException(errorMessage);
            }
        }

        public void RemoveStock(int warehouseId, List<StockAlteration> lineItems)
        {
            string sql = string.Format("UPDATE stock SET hld = hld - @hld WHERE w_id = {0} AND gtin_cd = @gtin_cd",
                warehouseId);

            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var lineItem in lineItems)
            {
                parametersList.Add(new NpgsqlParameter[]
                {
                    new NpgsqlParameter("@hld", lineItem.Quantity),
                    new NpgsqlParameter("@gtin_cd", lineItem.Gtin)
                });
            }

            base.RunTransaction(sql, parametersList);
        }
    }
}