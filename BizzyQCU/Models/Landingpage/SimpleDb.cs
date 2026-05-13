using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BizzyQCU.Models.Admin;

namespace BizzyQCU.Models.Landingpage
{
    // ========== MODEL CLASSES ==========
    public class StudentUserProfileData
    {
        public string Name { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string Section { get; set; }
        public string StudentNumber { get; set; }
        public DateTime? Birthdate { get; set; }
        public string Address { get; set; }
        public string PhotoDataUrl { get; set; }
    }

    public class EnterpriseProfileData
    {
        public int EnterpriseId { get; set; }
        public string StoreName { get; set; }
        public string EnterpriseType { get; set; }
        public string GcashNumber { get; set; }
        public string Email { get; set; }
        public string ManagerName { get; set; }
        public string ManagerStudentId { get; set; }
        public string ManagerContact { get; set; }
        public string Section { get; set; }
        public string StoreLogoPath { get; set; }
        public string QrDataUrl { get; set; }
    }

    public class EnterpriseDashboardStatsData
    {
        public int OrdersCompleted { get; set; }
        public int ProductsListed { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class EnterpriseDashboardStats
    {
        public decimal TotalSalesToday { get; set; }
        public int OrdersPending { get; set; }
        public int DeliveriesActive { get; set; }
        public int NewOrdersCount { get; set; }
    }

    public class WeeklySalesData
    {
        public string DayName { get; set; }
        public int DayOrder { get; set; }
        public decimal Sales { get; set; }
    }

    public class MonthlySalesData
    {
        public string MonthName { get; set; }
        public int MonthOrder { get; set; }
        public decimal Sales { get; set; }
    }

    // ========== ORDER MANAGEMENT MODELS ==========
    public class PendingOrder
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string DeliveryOption { get; set; }
        public string OrderNote { get; set; }
        public string Status { get; set; }
        public string OrderTime { get; set; }
        public string OrderDateFormatted { get; set; }
        public string Items { get; set; }
    }

    public class OrderDetails
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string DeliveryOption { get; set; }
        public string OrderNote { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderTime { get; set; }
        public string OrderDateFormatted { get; set; }
        public string EstimatedTime { get; set; }
        public string CustomerLocation { get; set; }
        public decimal DeliveryFee { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice;
    }

    public class TransactionHistoryItem
    {
        public string CustomerName { get; set; }
        public string Products { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }

    // ========== PRODUCT MODELS ==========
    public class ProductImage
    {
        public int ProductId { get; set; }
        public byte[] ImageData { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PreparationTime { get; set; }
        public string Status { get; set; }
        public bool HasImage { get; set; }
        public int? CategoryId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string StoreName { get; set; }
    }

    // ========== SIMPLEDB CLASS ==========
    public class SimpleDb
    {
        private string connectionString = "server=localhost;database=BizzyQCU;uid=root;pwd=;";
        private static bool orderStatusColumnChecked;

        private void EnsureOrderStatusColumn(MySqlConnection conn)
        {
            if (orderStatusColumnChecked)
            {
                return;
            }

            string sql = @"ALTER TABLE orders MODIFY status ENUM('pending','preparing','out_for_delivery','completed','cancelled') DEFAULT 'pending'";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }

            orderStatusColumnChecked = true;
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ========== GET ENTERPRISE BY USER ID ==========
        public Enterprises GetEnterpriseByUserId(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT enterprise_id, user_id, store_name, store_description, store_logo, contact_number, gcash_number, status FROM enterprises WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Enterprises
                                {
                                    EnterpriseId = reader.GetInt32("enterprise_id"),
                                    UserId = reader.GetInt32("user_id"),
                                    StoreName = reader.GetString("store_name"),
                                    StoreDescription = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                                    StoreLogo = reader.IsDBNull(reader.GetOrdinal("store_logo")) ? "" : reader.GetString("store_logo"),
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                    GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                    Status = reader.GetString("status")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        // ========== ADD PRODUCT WITH APPROVAL ==========
        public bool AddProductWithApproval(int enterpriseId, string productName, string description, decimal price, int? categoryId, int preparationTime, byte[] productImage, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO products (enterprise_id, category_id, product_name, description, price, preparation_time, product_image, status, is_approved, submitted_at, created_at) 
                                   VALUES (@enterpriseId, @categoryId, @productName, @description, @price, @preparationTime, @productImage, 'inactive', 0, NOW(), NOW())";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@productName", productName);
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@preparationTime", preparationTime);
                        cmd.Parameters.Add("@productImage", MySqlDbType.LongBlob).Value = productImage ?? (object)DBNull.Value;

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine("AddProductWithApproval error: " + ex.Message);
                return false;
            }
        }

        // ========== GET PRODUCT BY ID ==========
        public Product GetProductById(int productId, int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT product_id, product_name, description, price, preparation_time, status, category_id,
                                          CASE WHEN product_image IS NOT NULL THEN 1 ELSE 0 END AS has_image
                                   FROM products 
                                   WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Product
                                {
                                    ProductId = reader.GetInt32("product_id"),
                                    ProductName = reader.GetString("product_name"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                    Price = reader.GetDecimal("price"),
                                    PreparationTime = reader.IsDBNull(reader.GetOrdinal("preparation_time")) ? 0 : reader.GetInt32("preparation_time"),
                                    Status = reader.GetString("status"),
                                    HasImage = reader.GetInt32("has_image") == 1,
                                    CategoryId = reader.IsDBNull(reader.GetOrdinal("category_id")) ? (int?)null : reader.GetInt32("category_id")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductById error: " + ex.Message);
            }
            return null;
        }

        // ========== GET CATEGORY NAME BY ID ==========
        public string GetCategoryNameById(int? categoryId)
        {
            if (!categoryId.HasValue) return "";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT category_name FROM product_categories WHERE category_id = @categoryId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoryId", categoryId.Value);
                        var result = cmd.ExecuteScalar();
                        return result != null ? result.ToString() : "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetCategoryNameById error: " + ex.Message);
                return "";
            }
        }

        // ========== UPDATE PRODUCT ==========
        public bool UpdateProduct(int productId, int enterpriseId, string productName, string description, decimal price, int? categoryId, int preparationTime, byte[] productImage)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string sql;
                    if (productImage != null && productImage.Length > 0)
                    {
                        sql = @"UPDATE products SET product_name = @productName, description = @description, price = @price, 
                                category_id = @categoryId, preparation_time = @preparationTime, product_image = @productImage 
                                WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                    }
                    else
                    {
                        sql = @"UPDATE products SET product_name = @productName, description = @description, price = @price, 
                                category_id = @categoryId, preparation_time = @preparationTime 
                                WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                    }

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productName", productName);
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@preparationTime", preparationTime);
                        cmd.Parameters.AddWithValue("@productId", productId);
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);

                        if (productImage != null && productImage.Length > 0)
                        {
                            cmd.Parameters.Add("@productImage", MySqlDbType.LongBlob).Value = productImage;
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateProduct error: " + ex.Message);
                return false;
            }
        }

        // ========== GET PRODUCT IMAGE ==========
        public byte[] GetProductImage(int productId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT product_image FROM products WHERE product_id = @productId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return (byte[])result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductImage error: " + ex.Message);
            }
            return null;
        }

        // ========== GET PRODUCTS BY ENTERPRISE ID (APPROVED ONLY) ==========
        public List<Product> GetProductsByEnterpriseId(int enterpriseId)
        {
            var products = new List<Product>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT product_id, product_name, description, price, preparation_time, status, 
                                          CASE WHEN product_image IS NOT NULL THEN 1 ELSE 0 END AS has_image
                                   FROM products 
                                   WHERE enterprise_id = @enterpriseId AND status = 'active' AND is_approved = 1
                                   ORDER BY product_id DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    ProductId = reader.GetInt32("product_id"),
                                    ProductName = reader.GetString("product_name"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                    Price = reader.GetDecimal("price"),
                                    PreparationTime = reader.IsDBNull(reader.GetOrdinal("preparation_time")) ? 0 : reader.GetInt32("preparation_time"),
                                    Status = reader.GetString("status"),
                                    HasImage = reader.GetInt32("has_image") == 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductsByEnterpriseId error: " + ex.Message);
            }
            return products;
        }

        // ========== DELETE PRODUCT ==========
        public bool DeleteProduct(int productId, int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Try hard delete first.
                    string sql = "DELETE FROM products WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                    }

                    // If hard delete did not remove anything (or row is constrained by references),
                    // fallback to soft delete so it disappears from active listings.
                    string softDeleteSql = "UPDATE products SET status = 'inactive' WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                    using (var cmd = new MySqlCommand(softDeleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Common case: product is referenced by existing order_items.
                // Fallback to soft delete in a fresh command.
                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string softDeleteSql = "UPDATE products SET status = 'inactive' WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                        using (var cmd = new MySqlCommand(softDeleteSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@productId", productId);
                            cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    System.Diagnostics.Debug.WriteLine("DeleteProduct error: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("DeleteProduct soft delete fallback error: " + innerEx.Message);
                    return false;
                }
            }
        }

        // ========== GET OR CREATE CATEGORY ==========
        public int? GetOrCreateCategory(string categoryName)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string checkSql = "SELECT category_id FROM product_categories WHERE LOWER(category_name) = LOWER(@categoryName)";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoryName", categoryName);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                    }

                    string insertSql = "INSERT INTO product_categories (category_name) VALUES (@categoryName); SELECT LAST_INSERT_ID();";
                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoryName", categoryName);
                        int newId = Convert.ToInt32(cmd.ExecuteScalar());
                        return newId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetOrCreateCategory error: " + ex.Message);
                return null;
            }
        }

        // ========== GET PENDING ORDERS ==========
        public List<PendingOrder> GetPendingOrders(int enterpriseId)
        {
            var orders = new List<PendingOrder>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    EnsureOrderStatusColumn(conn);
                    string sql = @"
                        SELECT
                            o.order_id,
                            CONCAT(COALESCE(s.firstname, ''), ' ', COALESCE(s.lastname, '')) AS customer_name,
                            o.total_amount,
                            o.order_date,
                            o.delivery_option,
                            o.order_note,
                            o.status,
                            DATE_FORMAT(o.order_date, '%h:%i %p') AS order_time,
                            DATE_FORMAT(o.order_date, '%M %d, %Y') AS order_date_formatted,
                            GROUP_CONCAT(CONCAT(oi.quantity, 'x ', COALESCE(p.product_name, 'Unknown Product')) ORDER BY p.product_name SEPARATOR ', ') AS items
                        FROM orders o
                        INNER JOIN students s ON s.student_id = o.student_id
                        LEFT JOIN order_items oi ON oi.order_id = o.order_id
                        LEFT JOIN products p ON p.product_id = oi.product_id
                        WHERE o.enterprise_id = @enterpriseId
                          AND o.status IN ('pending', 'preparing', 'out_for_delivery')
                        GROUP BY o.order_id, s.firstname, s.lastname, o.total_amount, o.order_date, o.delivery_option, o.order_note, o.status
                        ORDER BY o.order_date DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var order = new PendingOrder();
                                order.OrderId = reader.GetInt32("order_id");
                                order.CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? "Customer" : reader.GetString("customer_name").Trim();
                                order.TotalAmount = reader.GetDecimal("total_amount");
                                order.OrderDate = reader.GetDateTime("order_date");
                                order.DeliveryOption = reader.IsDBNull(reader.GetOrdinal("delivery_option")) ? "pickup" : reader.GetString("delivery_option");
                                order.OrderNote = reader.IsDBNull(reader.GetOrdinal("order_note")) ? "" : reader.GetString("order_note");
                                order.Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status");
                                order.OrderTime = reader.IsDBNull(reader.GetOrdinal("order_time")) ? "" : reader.GetString("order_time");
                                order.OrderDateFormatted = reader.IsDBNull(reader.GetOrdinal("order_date_formatted")) ? "" : reader.GetString("order_date_formatted");
                                order.Items = reader.IsDBNull(reader.GetOrdinal("items")) ? "" : reader.GetString("items");
                                order.Status = reader.GetString("status");
                                order.CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? "Customer" : reader.GetString("customer_name").Trim();
                                if (string.IsNullOrWhiteSpace(order.CustomerName))
                                {
                                    order.CustomerName = "Customer";
                                }
                                orders.Add(order);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPendingOrders error: " + ex.Message);
            }
            return orders;
        }


        // ========== UPDATE ORDER STATUS ==========
        public bool UpdateOrderStatus(int orderId, string status, int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    EnsureOrderStatusColumn(conn);

                    // When order is confirmed (pending -> preparing), compute ETA dynamically:
                    // current time + total preparation minutes from ordered products.
                    if (string.Equals(status, "preparing", StringComparison.OrdinalIgnoreCase))
                    {
                        string etaSql = @"
                            UPDATE orders o
                            JOIN (
                                SELECT 
                                    oi.order_id,
                                    GREATEST(1, COALESCE(SUM(COALESCE(p.preparation_time, 0) * oi.quantity), 0)) AS total_prep_minutes
                                FROM order_items oi
                                LEFT JOIN products p ON p.product_id = oi.product_id
                                WHERE oi.order_id = @orderId
                                GROUP BY oi.order_id
                            ) prep ON prep.order_id = o.order_id
                            SET o.status = @status,
                                o.estimated_time = ADDTIME(CURTIME(), SEC_TO_TIME(prep.total_prep_minutes * 60))
                            WHERE o.order_id = @orderId AND o.enterprise_id = @enterpriseId";

                        using (var cmd = new MySqlCommand(etaSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", status.ToLower());
                            cmd.Parameters.AddWithValue("@orderId", orderId);
                            cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                    else
                    {
                        string sql = "UPDATE orders SET status = @status WHERE order_id = @orderId AND enterprise_id = @enterpriseId";
                        using (var cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", status.ToLower());
                            cmd.Parameters.AddWithValue("@orderId", orderId);
                            cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateOrderStatus error: " + ex.Message);
                return false;
            }
        }

        // ========== GET ORDER DETAILS ==========
        // ========== GET ORDER DETAILS ==========
        public OrderDetails GetOrderDetails(int orderId, int enterpriseId)
        {
            OrderDetails order = null;
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    o.order_id,
                    CONCAT(s.firstname, ' ', s.lastname) AS customer_name,
                    s.contact_number AS customer_phone,
                    o.total_amount,
                    o.delivery_option,
                    o.order_note,
                    o.status,
                    o.payment_method,
                    DATE_FORMAT(o.order_date, '%h:%i %p') AS order_time,
                    DATE_FORMAT(o.order_date, '%M %d, %Y') AS order_date_formatted,
                    TIME_FORMAT(o.estimated_time, '%h:%i %p') AS estimated_time_formatted,
                    o.customer_location
                FROM orders o
                INNER JOIN students s ON s.student_id = o.student_id
                        WHERE o.order_id = @orderId AND o.enterprise_id = @enterpriseId";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                order = new OrderDetails();
                                order.OrderId = reader.GetInt32("order_id");
                                order.CustomerName = reader.GetString("customer_name");
                                order.CustomerPhone = reader.GetString("customer_phone");
                                order.TotalAmount = reader.GetDecimal("total_amount");
                                order.DeliveryOption = reader.GetString("delivery_option");
                                order.OrderNote = reader.GetString("order_note");
                                order.Status = reader.GetString("status");
                                order.PaymentMethod = reader.GetString("payment_method");
                                order.OrderTime = reader.GetString("order_time");
                                order.OrderDateFormatted = reader.GetString("order_date_formatted");
                                order.EstimatedTime = reader.IsDBNull(reader.GetOrdinal("estimated_time_formatted")) ? "TBD" : reader.GetString("estimated_time_formatted");
                                order.CustomerLocation = reader.GetString("customer_location");
                                order.DeliveryFee = 0;  // I-set sa 0 palagi
                                order.Items = GetOrderItems(orderId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetOrderDetails error: " + ex.Message);
            }
            return order;
        }

        // ========== GET ORDER ITEMS ==========
        // ========== GET ORDER ITEMS ==========
        private List<OrderItem> GetOrderItems(int orderId)
        {
            var items = new List<OrderItem>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    COALESCE(p.product_name, 'Unknown Product') AS product_name,
                    oi.quantity,
                    oi.unit_price
                FROM order_items oi
                LEFT JOIN products p ON p.product_id = oi.product_id
                WHERE oi.order_id = @orderId";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OrderItem item = new OrderItem();
                                item.ProductName = reader.GetString("product_name");
                                item.Quantity = reader.GetInt32("quantity");
                                item.UnitPrice = reader.GetDecimal("unit_price");
                                items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetOrderItems error: " + ex.Message);
            }
            return items;
        }


        // ========== DASHBOARD STATS METHODS ==========
        public decimal GetTotalSalesToday(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COALESCE(SUM(total_amount), 0) FROM orders 
                                   WHERE enterprise_id = @enterpriseId AND status = 'completed' AND DATE(order_date) = CURDATE()";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetPendingOrdersCount(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) FROM orders WHERE enterprise_id = @enterpriseId AND status IN ('pending', 'preparing', 'out_for_delivery')";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetActiveDeliveriesCount(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) FROM orders WHERE enterprise_id = @enterpriseId 
                                   AND delivery_option != 'pickup' AND status = 'out_for_delivery'";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetNewOrdersCountToday(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) FROM orders WHERE enterprise_id = @enterpriseId AND DATE(order_date) = CURDATE() AND status IN ('pending', 'preparing', 'out_for_delivery')";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public List<WeeklySalesData> GetWeeklySalesData(int userId)
        {
            var result = new List<WeeklySalesData>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string getEnterpriseSql = "SELECT enterprise_id FROM enterprises WHERE user_id = @userId LIMIT 1";
                    int enterpriseId = 0;
                    using (var cmd = new MySqlCommand(getEnterpriseSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        object obj = cmd.ExecuteScalar();
                        if (obj != null) enterpriseId = Convert.ToInt32(obj);
                    }
                    if (enterpriseId == 0) return GetDefaultWeeklyData();

                    string sql = @"SELECT DAYNAME(order_date) AS DayName, DAYOFWEEK(order_date) AS DayOrder, COALESCE(SUM(total_amount), 0) AS Sales
                                   FROM orders WHERE enterprise_id = @enterpriseId AND status = 'completed'
                                   AND order_date >= DATE_SUB(CURDATE(), INTERVAL 6 DAY)
                                   GROUP BY DAYNAME(order_date), DAYOFWEEK(order_date) ORDER BY DAYOFWEEK(order_date)";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new WeeklySalesData
                                {
                                    DayName = reader.GetString("DayName"),
                                    DayOrder = reader.GetInt32("DayOrder"),
                                    Sales = reader.GetDecimal("Sales")
                                });
                            }
                        }
                    }
                    result = FillMissingWeekDays(result);
                }
            }
            catch (Exception)
            {
                return GetDefaultWeeklyData();
            }
            return result;
        }

        public List<MonthlySalesData> GetMonthlySalesData(int userId)
        {
            var result = new List<MonthlySalesData>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string getEnterpriseSql = "SELECT enterprise_id FROM enterprises WHERE user_id = @userId LIMIT 1";
                    int enterpriseId = 0;
                    using (var cmd = new MySqlCommand(getEnterpriseSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        object obj = cmd.ExecuteScalar();
                        if (obj != null) enterpriseId = Convert.ToInt32(obj);
                    }
                    if (enterpriseId == 0) return GetDefaultMonthlyData();

                    string sql = @"SELECT DATE_FORMAT(order_date, '%b') AS MonthName, MONTH(order_date) AS MonthOrder, COALESCE(SUM(total_amount), 0) AS Sales
                                   FROM orders WHERE enterprise_id = @enterpriseId AND status = 'completed'
                                   AND order_date >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH)
                                   GROUP BY DATE_FORMAT(order_date, '%b'), MONTH(order_date) ORDER BY MONTH(order_date)";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new MonthlySalesData
                                {
                                    MonthName = reader.GetString("MonthName"),
                                    MonthOrder = reader.GetInt32("MonthOrder"),
                                    Sales = reader.GetDecimal("Sales")
                                });
                            }
                        }
                    }
                    result = FillMissingMonths(result);
                }
            }
            catch (Exception)
            {
                return GetDefaultMonthlyData();
            }
            return result;
        }

        // ========== PRIVATE HELPER METHODS ==========
        private List<WeeklySalesData> FillMissingWeekDays(List<WeeklySalesData> data)
        {
            var allDays = new Dictionary<string, int> { { "Monday", 2 }, { "Tuesday", 3 }, { "Wednesday", 4 },
                { "Thursday", 5 }, { "Friday", 6 }, { "Saturday", 7 }, { "Sunday", 1 } };
            var result = new List<WeeklySalesData>();
            foreach (var day in allDays)
            {
                var existing = data.FirstOrDefault(x => x.DayName == day.Key);
                result.Add(existing ?? new WeeklySalesData { DayName = day.Key, DayOrder = day.Value, Sales = 0 });
            }
            return result.OrderBy(x => x.DayOrder).ToList();
        }

        private List<MonthlySalesData> FillMissingMonths(List<MonthlySalesData> data)
        {
            var allMonths = new Dictionary<string, int> { { "Jan", 1 }, { "Feb", 2 }, { "Mar", 3 }, { "Apr", 4 },
                { "May", 5 }, { "Jun", 6 }, { "Jul", 7 }, { "Aug", 8 }, { "Sep", 9 }, { "Oct", 10 }, { "Nov", 11 }, { "Dec", 12 } };
            var result = new List<MonthlySalesData>();
            int currentMonth = DateTime.Now.Month;
            int startMonth = currentMonth - 5;
            if (startMonth < 1) startMonth = 1;
            var last6Months = allMonths.Where(m => m.Value >= startMonth && m.Value <= currentMonth).OrderBy(m => m.Value).ToList();
            if (last6Months.Count < 6 && startMonth > 1)
            {
                startMonth = Math.Max(1, startMonth - (6 - last6Months.Count));
                last6Months = allMonths.Where(m => m.Value >= startMonth && m.Value <= currentMonth).OrderBy(m => m.Value).ToList();
            }
            foreach (var month in last6Months)
            {
                var existing = data.FirstOrDefault(x => x.MonthName == month.Key);
                result.Add(existing ?? new MonthlySalesData { MonthName = month.Key, MonthOrder = month.Value, Sales = 0 });
            }
            return result.OrderBy(x => x.MonthOrder).ToList();
        }

        private List<WeeklySalesData> GetDefaultWeeklyData()
        {
            return new List<WeeklySalesData>
            {
                new WeeklySalesData { DayName = "Monday", DayOrder = 2, Sales = 0 },
                new WeeklySalesData { DayName = "Tuesday", DayOrder = 3, Sales = 0 },
                new WeeklySalesData { DayName = "Wednesday", DayOrder = 4, Sales = 0 },
                new WeeklySalesData { DayName = "Thursday", DayOrder = 5, Sales = 0 },
                new WeeklySalesData { DayName = "Friday", DayOrder = 6, Sales = 0 },
                new WeeklySalesData { DayName = "Saturday", DayOrder = 7, Sales = 0 },
                new WeeklySalesData { DayName = "Sunday", DayOrder = 1, Sales = 0 }
            }.OrderBy(x => x.DayOrder).ToList();
        }

        private List<MonthlySalesData> GetDefaultMonthlyData()
        {
            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var result = new List<MonthlySalesData>();
            for (int i = 0; i < months.Length; i++)
            {
                result.Add(new MonthlySalesData { MonthName = months[i], MonthOrder = i + 1, Sales = 0 });
            }
            int currentMonth = DateTime.Now.Month;
            return result.Where(m => m.MonthOrder >= currentMonth - 5 && m.MonthOrder <= currentMonth).OrderBy(m => m.MonthOrder).ToList();
        }

        // ========== SUBMIT FEEDBACK ==========
        public bool SubmitFeedback(string email, string contactNumber, string userType, string category, string message, int rating, int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO feedbacks (user_id, user_type, email, contact_number, category, message, rating, status, created_at) 
                                   VALUES (@userId, @userType, @email, @contact, @category, @message, @rating, 'pending', NOW())";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@userType", userType);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        cmd.Parameters.AddWithValue("@category", category ?? "");
                        cmd.Parameters.AddWithValue("@message", message);
                        cmd.Parameters.AddWithValue("@rating", rating);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ========== GET USER BY USERNAME OR EMAIL ==========
        public Users GetUserByUsernameOrEmail(string username)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT user_id, username, password, email, role, is_approved FROM users WHERE (username = @username OR email = @username)";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Users
                                {
                                    UserId = reader.GetInt32("user_id"),
                                    Username = reader.GetString("username"),
                                    Password = reader.GetString("password"),
                                    Email = reader.GetString("email"),
                                    Role = reader.GetString("role"),
                                    IsApproved = reader.GetBoolean("is_approved")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        // ========== ADDITIONAL METHODS FOR PROFILE ==========
        public EnterpriseProfileData GetEnterpriseProfileDataByUserId(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT e.enterprise_id, e.store_name, e.enterprise_type, e.gcash_number, e.store_logo, u.email,
                                          em.manager_name, em.manager_student_id, em.manager_contact, em.manager_section
                                   FROM users u
                                   INNER JOIN enterprises e ON e.user_id = u.user_id
                                   LEFT JOIN enterprise_managers em ON em.enterprise_id = e.enterprise_id
                                   WHERE u.user_id = @userId
                                   LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new EnterpriseProfileData
                                {
                                    EnterpriseId = reader.GetInt32("enterprise_id"),
                                    StoreName = reader.IsDBNull(reader.GetOrdinal("store_name")) ? "" : reader.GetString("store_name"),
                                    EnterpriseType = reader.IsDBNull(reader.GetOrdinal("enterprise_type")) ? "" : reader.GetString("enterprise_type"),
                                    GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                    StoreLogoPath = reader.IsDBNull(reader.GetOrdinal("store_logo")) ? "" : reader.GetString("store_logo"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                                    ManagerName = reader.IsDBNull(reader.GetOrdinal("manager_name")) ? "" : reader.GetString("manager_name"),
                                    ManagerStudentId = reader.IsDBNull(reader.GetOrdinal("manager_student_id")) ? "" : reader.GetString("manager_student_id"),
                                    ManagerContact = reader.IsDBNull(reader.GetOrdinal("manager_contact")) ? "" : reader.GetString("manager_contact"),
                                    Section = reader.IsDBNull(reader.GetOrdinal("manager_section")) ? "" : reader.GetString("manager_section")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public EnterpriseDashboardStatsData GetEnterpriseDashboardStatsByUserId(int userId)
        {
            var result = new EnterpriseDashboardStatsData();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT COUNT(*) as orders_completed FROM orders o
                                   INNER JOIN enterprises e ON e.enterprise_id = o.enterprise_id
                                   WHERE e.user_id = @userId AND o.status = 'completed'";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        result.OrdersCompleted = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception)
            {
                return result;
            }
            return result;
        }

        public List<TransactionHistoryItem> GetTransactionHistoryByUserId(int userId)
        {
            var transactions = new List<TransactionHistoryItem>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT
                            CONCAT(COALESCE(s.firstname, ''), ' ', COALESCE(s.lastname, '')) AS customer_name,
                            GROUP_CONCAT(CONCAT(oi.quantity, 'x ', COALESCE(p.product_name, 'Unknown Product')) ORDER BY p.product_name SEPARATOR ', ') AS products,
                            o.order_date,
                            o.total_amount,
                            o.status
                        FROM orders o
                        INNER JOIN enterprises e ON e.enterprise_id = o.enterprise_id
                        INNER JOIN students s ON s.student_id = o.student_id
                        INNER JOIN order_items oi ON oi.order_id = o.order_id
                        LEFT JOIN products p ON p.product_id = oi.product_id
                        WHERE e.user_id = @userId
                          AND o.status IN ('completed', 'cancelled')
                        GROUP BY o.order_id, s.firstname, s.lastname, o.order_date, o.total_amount, o.status
                        ORDER BY o.order_date DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                transactions.Add(new TransactionHistoryItem
                                {
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name"))
                                        ? "Customer"
                                        : reader.GetString("customer_name").Trim(),
                                    Products = reader.IsDBNull(reader.GetOrdinal("products"))
                                        ? "No products"
                                        : reader.GetString("products"),
                                    OrderDate = reader.GetDateTime("order_date"),
                                    TotalAmount = reader.GetDecimal("total_amount"),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status"))
                                        ? string.Empty
                                        : reader.GetString("status")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetTransactionHistoryByUserId error: " + ex.Message);
            }

            return transactions;
        }

        public StudentUserProfileData GetEnterpriseUserProfileByUserId(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT u.username, u.email, e.store_name, e.contact_number
                                   FROM users u
                                   LEFT JOIN enterprises e ON e.user_id = u.user_id
                                   WHERE u.user_id = @userId LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentUserProfileData
                                {
                                    Name = reader.IsDBNull(reader.GetOrdinal("store_name")) ? reader.GetString("username") : reader.GetString("store_name"),
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public StudentUserProfileData GetStudentUserProfileByUserId(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT u.username, u.email, s.firstname, s.lastname, s.contact_number, s.section, s.student_number,
                                          s.birthdate, s.address, s.profile_image
                                   FROM users u
                                   LEFT JOIN students s ON s.user_id = u.user_id
                                   WHERE u.user_id = @userId LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string firstName = reader.IsDBNull(reader.GetOrdinal("firstname")) ? "" : reader.GetString("firstname");
                                string lastName = reader.IsDBNull(reader.GetOrdinal("lastname")) ? "" : reader.GetString("lastname");
                                string username = reader.GetString("username");
                                return new StudentUserProfileData
                                {
                                    Name = string.IsNullOrEmpty(firstName) ? username : $"{firstName} {lastName}",
                                    Firstname = firstName,
                                    Lastname = lastName,
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                    Email = reader.GetString("email"),
                                    Section = reader.IsDBNull(reader.GetOrdinal("section")) ? "" : reader.GetString("section"),
                                    StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? "" : reader.GetString("student_number"),
                                    Birthdate = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? (DateTime?)null : reader.GetDateTime("birthdate"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? "" : reader.GetString("address"),
                                    PhotoDataUrl = reader.IsDBNull(reader.GetOrdinal("profile_image"))
                                        ? ""
                                        : BuildImageDataUri((byte[])reader["profile_image"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public bool IsUsernameRequestExists(string username, string role = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return false;
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM approval_requests WHERE username = @username AND LOWER(status) = 'pending'";
                    if (!string.IsNullOrWhiteSpace(role)) sql += " AND role = @role";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        if (!string.IsNullOrWhiteSpace(role)) cmd.Parameters.AddWithValue("@role", role);
                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception) { return false; }
        }

        public bool IsEmailRequestExists(string email, string role = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return false;
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM approval_requests WHERE email = @email AND LOWER(status) = 'pending'";
                    if (!string.IsNullOrWhiteSpace(role)) sql += " AND role = @role";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        if (!string.IsNullOrWhiteSpace(role)) cmd.Parameters.AddWithValue("@role", role);
                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception) { return false; }
        }

        public bool SubmitStudentRequest(string firstName, string lastName, string username, string email, string password, string birthdate, string studentNumber, string section, string contactNumber, byte[] qcuIdBytes = null)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO approval_requests (username, password, email, role, firstname, lastname, birthdate, student_number, section, contact_number, qcu_id, status) 
                                   VALUES (@username, @password, @email, 'student', @firstname, @lastname, @birthdate, @studentNumber, @section, @contact, @qcuId, 'pending')";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@firstname", firstName ?? "");
                        cmd.Parameters.AddWithValue("@lastname", lastName ?? "");
                        cmd.Parameters.AddWithValue("@birthdate", string.IsNullOrEmpty(birthdate) ? DBNull.Value : (object)birthdate);
                        cmd.Parameters.AddWithValue("@studentNumber", studentNumber ?? "");
                        cmd.Parameters.AddWithValue("@section", section ?? "");
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        if (qcuIdBytes == null || qcuIdBytes.Length == 0)
                        {
                            cmd.Parameters.Add("@qcuId", MySqlDbType.Blob).Value = DBNull.Value;
                        }
                        else
                        {
                            cmd.Parameters.Add("@qcuId", MySqlDbType.Blob).Value = qcuIdBytes;
                        }
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public bool SubmitEnterpriseRequest(string storeName, string enterpriseType, string username, string email, string password, string contactNumber, string gcashNumber, byte[] uploadedDocumentBytes = null)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO approval_requests (username, password, email, role, store_name, store_description, contact_number, gcash_number, uploaded_document, status) 
                                   VALUES (@username, @password, @email, 'enterprise', @storeName, @storeDesc, @contact, @gcash, @uploadedDocument, 'pending')";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@storeName", storeName ?? "");
                        cmd.Parameters.AddWithValue("@storeDesc", enterpriseType ?? "");
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        cmd.Parameters.AddWithValue("@gcash", gcashNumber ?? "");
                        if (uploadedDocumentBytes == null || uploadedDocumentBytes.Length == 0)
                        {
                            cmd.Parameters.Add("@uploadedDocument", MySqlDbType.Blob).Value = DBNull.Value;
                        }
                        else
                        {
                            cmd.Parameters.Add("@uploadedDocument", MySqlDbType.Blob).Value = uploadedDocumentBytes;
                        }
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public bool UpdateEnterpriseProfile(int userId, string storeName, string storeDescription, string contactNumber, string gcashNumber)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE enterprises SET store_name = @storeName, store_description = @storeDesc, contact_number = @contact, gcash_number = @gcash WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@storeName", storeName ?? "");
                        cmd.Parameters.AddWithValue("@storeDesc", storeDescription ?? "");
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        cmd.Parameters.AddWithValue("@gcash", gcashNumber ?? "");
                        cmd.Parameters.AddWithValue("@userId", userId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception) { return false; }
        }

        public bool UpdateEnterpriseUserProfile(int userId, string nameOrStoreName, string contactNumber, string email)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE enterprises SET store_name = @storeName, contact_number = @contact WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@storeName", nameOrStoreName ?? "");
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                    string userSql = "UPDATE users SET email = @email WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(userSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email ?? "");
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public bool UpdateStudentUserProfile(int userId, string firstName, string lastName, string studentNumber, string section, DateTime? birthdate, string address, string contactNumber, string email, string photoDataUrl)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    int studentCount;
                    using (var countCmd = new MySqlCommand("SELECT COUNT(*) FROM students WHERE user_id = @userId", conn))
                    {
                        countCmd.Parameters.AddWithValue("@userId", userId);
                        studentCount = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    byte[] profileImageBytes = TryParseImageDataUrl(photoDataUrl);
                    bool hasNewProfileImage = profileImageBytes != null && profileImageBytes.Length > 0;

                    string sql = studentCount > 0
                        ? @"UPDATE students
                            SET firstname = @firstname,
                                lastname = @lastname,
                                student_number = @studentNumber,
                                section = @section,
                                birthdate = @birthdate,
                                address = @address,
                                contact_number = @contact" + (hasNewProfileImage ? @",
                                profile_image = @profileImage" : "") + @"
                            WHERE user_id = @userId"
                        : @"INSERT INTO students
                            (user_id, firstname, lastname, student_number, section, birthdate, address, contact_number, profile_image)
                            VALUES
                            (@userId, @firstname, @lastname, @studentNumber, @section, @birthdate, @address, @contact, @profileImage)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@firstname", firstName ?? "");
                        cmd.Parameters.AddWithValue("@lastname", lastName ?? "");
                        cmd.Parameters.AddWithValue("@studentNumber", studentNumber ?? "");
                        cmd.Parameters.AddWithValue("@section", section ?? "");
                        cmd.Parameters.AddWithValue("@birthdate", birthdate.HasValue ? (object)birthdate.Value.Date : DBNull.Value);
                        cmd.Parameters.AddWithValue("@address", address ?? "");
                        cmd.Parameters.AddWithValue("@contact", contactNumber ?? "");
                        cmd.Parameters.AddWithValue("@userId", userId);
                        if (hasNewProfileImage || studentCount == 0)
                        {
                            cmd.Parameters.AddWithValue("@profileImage", hasNewProfileImage ? (object)profileImageBytes : DBNull.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    string userSql = "UPDATE users SET email = @email WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(userSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email ?? "");
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        private byte[] TryParseImageDataUrl(string dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                return null;
            }

            const string base64Marker = ";base64,";
            int markerIndex = dataUrl.IndexOf(base64Marker, StringComparison.OrdinalIgnoreCase);
            if (!dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) || markerIndex < 0)
            {
                return null;
            }

            string base64 = dataUrl.Substring(markerIndex + base64Marker.Length);
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private string BuildImageDataUri(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";

            string mime = "image/jpeg";
            if (bytes.Length >= 4)
            {
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) mime = "image/png";
                else if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) mime = "image/jpeg";
                else if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) mime = "image/gif";
                else if (bytes[0] == 0x42 && bytes[1] == 0x4D) mime = "image/bmp";
                else if (bytes.Length > 11 &&
                    bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                    bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) mime = "image/webp";
            }

            return "data:" + mime + ";base64," + Convert.ToBase64String(bytes);
        }

        public bool SaveEnterpriseProfileData(int userId, string storeName, string enterpriseType, string gcashNumber, string email, string managerName, string managerStudentId, string managerContact, string section, string storeLogoPath, byte[] qrBytes)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        int enterpriseId = 0;
                        string enterpriseIdSql = "SELECT enterprise_id FROM enterprises WHERE user_id = @userId LIMIT 1";
                        using (var idCmd = new MySqlCommand(enterpriseIdSql, conn, tx))
                        {
                            idCmd.Parameters.AddWithValue("@userId", userId);
                            var idResult = idCmd.ExecuteScalar();
                            if (idResult == null || idResult == DBNull.Value)
                            {
                                tx.Rollback();
                                return false;
                            }
                            enterpriseId = Convert.ToInt32(idResult);
                        }

                        string enterpriseSql = @"UPDATE enterprises
                                                 SET store_name = @storeName,
                                                     enterprise_type = @enterpriseType,
                                                     gcash_number = @gcashNumber,
                                                     store_logo = COALESCE(@storeLogoPath, store_logo)
                                                 WHERE user_id = @userId";
                        using (var enterpriseCmd = new MySqlCommand(enterpriseSql, conn, tx))
                        {
                            enterpriseCmd.Parameters.AddWithValue("@storeName", storeName ?? "");
                            enterpriseCmd.Parameters.AddWithValue("@enterpriseType", enterpriseType ?? "");
                            enterpriseCmd.Parameters.AddWithValue("@gcashNumber", gcashNumber ?? "");
                            enterpriseCmd.Parameters.AddWithValue("@storeLogoPath", string.IsNullOrWhiteSpace(storeLogoPath) ? (object)DBNull.Value : storeLogoPath);
                            enterpriseCmd.Parameters.AddWithValue("@userId", userId);
                            enterpriseCmd.ExecuteNonQuery();
                        }

                        string userSql = @"UPDATE users SET email = @email WHERE user_id = @userId";
                        using (var userCmd = new MySqlCommand(userSql, conn, tx))
                        {
                            userCmd.Parameters.AddWithValue("@email", email ?? "");
                            userCmd.Parameters.AddWithValue("@userId", userId);
                            userCmd.ExecuteNonQuery();
                        }

                        string managerCountSql = "SELECT COUNT(*) FROM enterprise_managers WHERE enterprise_id = @enterpriseId";
                        long managerCount;
                        using (var countCmd = new MySqlCommand(managerCountSql, conn, tx))
                        {
                            countCmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                            managerCount = (long)countCmd.ExecuteScalar();
                        }

                        if (managerCount > 0)
                        {
                            string updateManagerSql = @"UPDATE enterprise_managers
                                                        SET manager_name = @managerName,
                                                            manager_student_id = @managerStudentId,
                                                            manager_contact = @managerContact,
                                                            manager_section = @managerSection
                                                        WHERE enterprise_id = @enterpriseId";
                            using (var updateManagerCmd = new MySqlCommand(updateManagerSql, conn, tx))
                            {
                                updateManagerCmd.Parameters.AddWithValue("@managerName", managerName ?? "");
                                updateManagerCmd.Parameters.AddWithValue("@managerStudentId", managerStudentId ?? "");
                                updateManagerCmd.Parameters.AddWithValue("@managerContact", managerContact ?? "");
                                updateManagerCmd.Parameters.AddWithValue("@managerSection", section ?? "");
                                updateManagerCmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                updateManagerCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertManagerSql = @"INSERT INTO enterprise_managers
                                                        (enterprise_id, manager_name, manager_student_id, manager_contact, manager_section)
                                                        VALUES
                                                        (@enterpriseId, @managerName, @managerStudentId, @managerContact, @managerSection)";
                            using (var insertManagerCmd = new MySqlCommand(insertManagerSql, conn, tx))
                            {
                                insertManagerCmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                insertManagerCmd.Parameters.AddWithValue("@managerName", managerName ?? "");
                                insertManagerCmd.Parameters.AddWithValue("@managerStudentId", managerStudentId ?? "");
                                insertManagerCmd.Parameters.AddWithValue("@managerContact", managerContact ?? "");
                                insertManagerCmd.Parameters.AddWithValue("@managerSection", section ?? "");
                                insertManagerCmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }
    }
}
