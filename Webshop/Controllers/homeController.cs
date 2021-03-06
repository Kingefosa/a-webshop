﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;

namespace Annytab.Webshop.Controllers
{
    /// <summary>
    /// This class controls the default (home) page of the website
    /// </summary>
    public class homeController : Controller
    {
        #region View methods
        
        // Get the default page
        // GET: /home/
        [HttpGet]
        public ActionResult index()
        {
            // Get the current domain and the home page
            Domain currentDomain = Tools.GetCurrentDomain();
            StaticPage homePage = StaticPage.GetOneByConnectionId(1, currentDomain.front_end_language);
            homePage = homePage != null ? homePage : new StaticPage();

            // Set form values
            ViewBag.CurrentCategory = new Category();
            ViewBag.BreadCrumbs = new List<BreadCrumb>(0);
            ViewBag.TranslatedTexts = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.HomePage = homePage;
            ViewBag.UserSettings = (Dictionary<string, string>)Session["UserSettings"];
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/home.cshtml");

        } // End of the index method

        // Get the search result page
        // GET: /home/search/
        [HttpGet]
        public ActionResult search()
        {
            // Get the current domain and the home page
            Domain currentDomain = Tools.GetCurrentDomain();

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(2);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            breadCrumbs.Add(new BreadCrumb(tt.Get("search_result"), "/home/search"));

            // Set form values
            ViewBag.CurrentCategory = new Category();
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.UserSettings = (Dictionary<string, string>)Session["UserSettings"];
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/search.cshtml");

        } // End of the search method

        // Get the category page
        // GET: /home/category/
        [HttpGet]
        public ActionResult category(string id = "")
        {
            // Get the domain and the current category
            Domain currentDomain = Tools.GetCurrentDomain();
            Category currentCategory = Category.GetOneByPageName(id, currentDomain.front_end_language);

            // Make sure that the category not is null
            if (currentCategory == null)
            {
                Response.StatusCode = 404;
                Response.Status = "404 Not Found";
                Response.Write(Tools.GetHttpNotFoundPage());
                return new EmptyResult();
            }
                
            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Get a chain of parent categories
            List<Category> parentCategoryChain = Category.GetParentCategoryChain(currentCategory, currentDomain.front_end_language);

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(10);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            for (int i = 0; i < parentCategoryChain.Count; i++)
            {
                breadCrumbs.Add(new BreadCrumb(parentCategoryChain[i].title, "/home/category/" + parentCategoryChain[i].page_name));
            }

            // Update page views
            if (currentCategory.page_views <= Int32.MaxValue - 1)
            {
                Category.UpdatePageviews(currentCategory.id, currentCategory.page_views + 1);
            }

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.CurrentCategory = currentCategory;
            ViewBag.UserSettings = (Dictionary<string, string>)Session["UserSettings"];
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/category.cshtml");

        } // End of the category method

        // Get the product page
        // GET: /home/product/
        [HttpGet]
        public ActionResult product(string id = "")
        {
            // Get the domain, the product, the currency and the value added tax
            Domain currentDomain = Tools.GetCurrentDomain();
            Product currentProduct = Product.GetOneByPageName(id, currentDomain.front_end_language);
            
            // Make sure that the product not is null
            if (currentProduct == null)
            {
                Response.StatusCode = 404;
                Response.Status = "404 Not Found";
                Response.Write(Tools.GetHttpNotFoundPage());
                return new EmptyResult();
            }

            // Get additional data
            Currency currency = Currency.GetOneById(currentDomain.currency);
            ValueAddedTax valueAddedTax = ValueAddedTax.GetOneById(currentProduct.value_added_tax_id);
            Category currentCategory = Category.GetOneById(currentProduct.category_id, currentDomain.front_end_language);
            currentCategory = currentCategory != null ? currentCategory : new Category();

            // Get the product price and product code
            decimal productPrice = currentProduct.unit_price;
            string productCode = currentProduct.product_code;
            string manufacturerCode = currentProduct.manufacturer_code;
            string variantImageUrl = currentProduct.variant_image_filename;

            // Get product option types and product options
            List<ProductOptionType> productOptionTypes = ProductOptionType.GetByProductId(currentProduct.id, currentDomain.front_end_language);
            Dictionary<Int32, List<ProductOption>> productOptions = new Dictionary<int, List<ProductOption>>(productOptionTypes.Count);

            // Loop all the product option types
            for (int i = 0; i < productOptionTypes.Count; i++)
            {
                List<ProductOption> listProductOptions = ProductOption.GetByProductOptionTypeId(productOptionTypes[i].id, currentDomain.front_end_language);

                if (listProductOptions.Count > 0)
                {
                    productPrice += listProductOptions[0].price_addition;
                    productCode += listProductOptions[0].product_code_suffix;
                    manufacturerCode += listProductOptions[0].mpn_suffix;
                    variantImageUrl = variantImageUrl.Replace("[" + i + "]", listProductOptions[0].product_code_suffix);
                }

                // Add all the product options for the option type to the dictionary
                productOptions.Add(productOptionTypes[i].option_type_id, ProductOption.GetByProductOptionTypeId(productOptionTypes[i].id, currentDomain.front_end_language));
            }

            // Adjust the product price with the currency conversion rate
            productPrice *= (currency.currency_base / currency.conversion_rate);

            // Round the price to the minor unit for the currency
            Int32 decimalMultiplier = (Int32)Math.Pow(10, currency.decimals);
            decimal ordinaryPrice = Math.Round(productPrice * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
            productPrice = Math.Round(productPrice * (1 - currentProduct.discount) * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;

            // Check if prices should include vat
            bool pricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;

            // Add vat if prices should include vat
            if (pricesIncludesVat == true)
            {
                ordinaryPrice += Math.Round(ordinaryPrice * valueAddedTax.value * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
                productPrice += Math.Round(productPrice * valueAddedTax.value * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
            }

            // Calculate the comparison price
            decimal comparisonPrice = 0;
            if (currentProduct.unit_pricing_measure > 0 && currentProduct.unit_pricing_base_measure > 0)
            {
                comparisonPrice = (currentProduct.unit_pricing_base_measure / currentProduct.unit_pricing_measure) * productPrice;
                comparisonPrice = Math.Round(comparisonPrice * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
            }

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Get a chain of parent categories
            List<Category> parentCategoryChain = Category.GetParentCategoryChain(currentCategory, currentDomain.front_end_language);

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(10);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            for (int i = 0; i < parentCategoryChain.Count; i++)
            {
                breadCrumbs.Add(new BreadCrumb(parentCategoryChain[i].title, "/home/category/" + parentCategoryChain[i].page_name));
            }
            breadCrumbs.Add(new BreadCrumb(currentProduct.title, "/home/product/" + currentProduct.page_name));

            // Update page views
            if (currentProduct.page_views <= Int32.MaxValue - 1)
            {
                Product.UpdatePageviews(currentProduct.id, currentProduct.page_views + 1);
            }

            // Get the unit
            Unit unit = Unit.GetOneById(currentProduct.unit_id, currentDomain.front_end_language);

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.CurrentCategory = currentCategory;
            ViewBag.Currency = currency;
            ViewBag.CurrentProduct = currentProduct;
            ViewBag.ValueAddedTax = valueAddedTax;
            ViewBag.ProductOptionTypes = productOptionTypes;
            ViewBag.ProductOptions = productOptions;
            ViewBag.ProductPrice = productPrice;
            ViewBag.OrdinaryPrice = ordinaryPrice;
            ViewBag.ComparisonPrice = comparisonPrice;
            ViewBag.Unit = unit != null ? unit : new Unit();
            ViewBag.ProductCode = productCode;
            ViewBag.ManufacturerCode = manufacturerCode;
            ViewBag.VariantImageUrl = variantImageUrl;
            ViewBag.UserSettings = (Dictionary<string, string>)Session["UserSettings"];
            ViewBag.PricesIncludesVat = pricesIncludesVat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/product.cshtml");

        } // End of the product method

        // Get the information page
        // GET: /home/information/
        [HttpGet]
        public ActionResult information(string id = "")
        {
            // Get the current domain and the static page
            Domain currentDomain = Tools.GetCurrentDomain();
            StaticPage staticPage = StaticPage.GetOneByPageName(id, currentDomain.front_end_language);

            // Make sure that the static page not is null
            if(staticPage == null)
            {
                Response.StatusCode = 404;
                Response.Status = "404 Not Found";
                Response.Write(Tools.GetHttpNotFoundPage());
                return new EmptyResult();
            }

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(2);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            breadCrumbs.Add(new BreadCrumb(staticPage.link_name, "/home/information/" + staticPage.page_name));

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentCategory = new Category();
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.StaticPage = staticPage;
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/information.cshtml");

        } // End of the information method

        // Get the contact us page
        // GET: /home/contact_us/
        [HttpGet]
        public ActionResult contact_us()
        {
            // Get the current domain
            Domain currentDomain = Tools.GetCurrentDomain();

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(2);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            breadCrumbs.Add(new BreadCrumb(tt.Get("contact_us"), "/home/contact_us"));

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentCategory = new Category();
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.Company = Company.GetOneById(currentDomain.company_id);
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/contact_us.cshtml");

        } // End of the contact_us method

        // Get the terms of purchase page
        // GET: /home/terms_of_purchase/
        [HttpGet]
        public ActionResult terms_of_purchase()
        {
            // Get the current domain and the static page
            Domain currentDomain = Tools.GetCurrentDomain();
            StaticPage staticPage = StaticPage.GetOneByConnectionId(2, currentDomain.front_end_language);
            staticPage = staticPage != null ? staticPage : new StaticPage();

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(2);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            breadCrumbs.Add(new BreadCrumb(staticPage.link_name, "/home/terms_of_purchase"));

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentCategory = new Category();
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.StaticPage = staticPage;
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/terms_of_purchase.cshtml");

        } // End of the terms_of_purchase method

        // Get the error page
        // GET: /home/error/
        [HttpGet]
        public ActionResult error(string id = "")
        {
            // Get the current domain
            Domain currentDomain = Tools.GetCurrentDomain();

            // Set the connection id
            byte connectionId = 3;
            if (id == "invalid-input")
            {
                connectionId = 4;
            }
            else if (id == "404")
            {
                connectionId = 5;
            }            
            else
            {
                id = "general";
            }
               
            // Get the error page
            StaticPage staticPage = StaticPage.GetOneByConnectionId(connectionId, currentDomain.front_end_language);
            staticPage = staticPage != null ? staticPage : new StaticPage();

            // Get the translated texts
            KeyStringList tt = StaticText.GetAll(currentDomain.front_end_language, "id", "ASC");

            // Create the bread crumb list
            List<BreadCrumb> breadCrumbs = new List<BreadCrumb>(2);
            breadCrumbs.Add(new BreadCrumb(tt.Get("start_page"), "/"));
            breadCrumbs.Add(new BreadCrumb(staticPage.link_name, "/home/error/" + id.ToString()));

            // Set form values
            ViewBag.BreadCrumbs = breadCrumbs;
            ViewBag.CurrentCategory = new Category();
            ViewBag.TranslatedTexts = tt;
            ViewBag.CurrentDomain = currentDomain;
            ViewBag.CurrentLanguage = Language.GetOneById(currentDomain.front_end_language);
            ViewBag.StaticPage = staticPage;
            ViewBag.PricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : currentDomain.prices_includes_vat;
            ViewBag.CultureInfo = Tools.GetCultureInfo(ViewBag.CurrentLanguage);

            // Return the view
            return currentDomain.custom_theme_id == 0 ? View() : View("/Views/theme/error.cshtml");

        } // End of the error method

        // Update the click count and redirect the user to the campaign url
        // GET: /home/campaign_tracker/2
        [HttpGet]
        public ActionResult campaign_tracker(Int32 id = 0)
        {
            // Get the campaign
            Campaign campaign = Campaign.GetOneById(id);

            // Make sure that the campaign not is null
            if (campaign == null)
            {
                Response.StatusCode = 404;
                Response.Status = "404 Not Found";
                Response.Write(Tools.GetHttpNotFoundPage());
                return new EmptyResult();
            }

            // Calculate the new click count
            Int32 count = campaign.click_count + 1;

            // Update the count
            Campaign.UpdateClickCount(campaign.id, count);

            // Redirect the user to the campaign url
            return Redirect(campaign.link_url);

        } // End of the campaign_tracker method

        #endregion

        #region Post methods

        // Search for products
        // POST: /home/search
        [HttpPost]
        public ActionResult search(FormCollection collection)
        {
            // Get the keywords
            string keywordString = collection["txtSearch"];

            // Return the url with search parameters
            return Redirect("/home/search?kw=" + Server.UrlEncode(keywordString));

        } // End of the search method

        // Add the product to the shopping cart
        // POST: /home/add_product
        [HttpPost]
        public ActionResult add_product(FormCollection collection)
        {
            // Get the product id
            Int32 productId = Convert.ToInt32(collection["hiddenProductId"]);

            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the currency
            Currency currency = Currency.GetOneById(domain.currency);

            // Get the product by id
            Product product = Product.GetOneById(productId, domain.front_end_language);

            // Get translated texts
            KeyStringList tt = StaticText.GetAll(domain.front_end_language, "id", "ASC");

            // Make sure that the product not is null
            if (product == null)
            {
                Response.StatusCode = 404;
                Response.Status = "404 Not Found";
                Response.Write(Tools.GetHttpNotFoundPage());
                return new EmptyResult();
            }

            // Get form data
            decimal quantity = 0;
            decimal.TryParse(collection["txtQuantity"].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);
            string[] productOptionTypeIds = collection.GetValues("productOptionTypeId");
            string[] selectedOptionIds = collection.GetValues("selectProductOption");

            // Get other values
            string productName = product.title;
            string productCode = product.product_code;
            string manufacturerCode = product.manufacturer_code;
            decimal unitPrice = product.unit_price;
            decimal unitFreight = product.unit_freight + product.toll_freight_addition;
            string variantImageUrl = product.variant_image_filename;

            // Update the added to cart statistic
            if(product.added_in_basket <= Int32.MaxValue - 1)
            {
                Product.UpdateAddedInBasket(product.id, product.added_in_basket + 1);
            }

            // Check if the product has a affiliate link
            if (product.affiliate_link != "")
            {
                // Redirect the user to the affiliate site
                return Redirect(product.affiliate_link);
            }

            // The count of option ids
            Int32 optionIdCount = selectedOptionIds != null ? selectedOptionIds.Length : 0;

            // Loop option identities and add to the price, freight and product code
            for (int i = 0; i < optionIdCount; i++)
            {
                // Convert the ids
                Int32 optionTypeId = Convert.ToInt32(productOptionTypeIds[i]);
                Int32 optionId = Convert.ToInt32(selectedOptionIds[i]);

                // Get the product option type and the product option
                ProductOptionType productOptionType = ProductOptionType.GetOneById(optionTypeId, domain.front_end_language);
                ProductOption productOption = ProductOption.GetOneById(optionTypeId, optionId, domain.front_end_language);

                // Add to values
                productName += "," + productOptionType.title + ": " + productOption.title;
                productCode += productOption.product_code_suffix;
                manufacturerCode += productOption.mpn_suffix;
                unitPrice += productOption.price_addition;
                unitFreight += productOption.freight_addition;
                variantImageUrl = variantImageUrl.Replace("[" + i + "]", productOption.product_code_suffix);
            }

            // Add delivery time to the product name
            productName += "," + tt.Get("delivery_time") + ": " + product.delivery_time;

            // Adjust the price and the freight with the conversion rate
            unitPrice *= (currency.currency_base / currency.conversion_rate);
            unitFreight *= (currency.currency_base / currency.conversion_rate);

            // Round the price to the minor unit for the currency
            Int32 decimalMultiplier = (Int32)Math.Pow(10, currency.decimals);
            unitPrice = Math.Round(unitPrice * (1 - product.discount) * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
            unitFreight = Math.Round(unitFreight * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;

            // Get the value added tax
            ValueAddedTax vat = ValueAddedTax.GetOneById(product.value_added_tax_id);

            // Create a cart item
            CartItem cartItem = new CartItem();
            cartItem.product_code = productCode;
            cartItem.product_id = product.id;
            cartItem.manufacturer_code = manufacturerCode;
            cartItem.product_name = productName;
            cartItem.quantity = quantity;
            cartItem.unit_price = unitPrice;
            cartItem.unit_freight = unitFreight;
            cartItem.vat_percent = vat.value;
            cartItem.variant_image_url = variantImageUrl;
            cartItem.use_local_images = product.use_local_images;

            // Add the cart item to the shopping cart
            CartItem.AddToShoppingCart(cartItem);

            // Redirect the user to the same product page
            return RedirectToAction("product", "home", new { id = product.page_name, cu = "true" });

        } // End of the add_product method

        // Add to the cart
        // POST: /home/add_to_cart
        [HttpPost]
        public ActionResult add_to_cart(FormCollection collection)
        {
            // Get form data
            Int32 productId = Convert.ToInt32(collection["pid"]);
            decimal quantity = 0;
            decimal.TryParse(collection["qty"].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);
            string[] productOptionTypeIds = collection["optionTypes"].Split('|');
            string[] selectedOptionIds = collection["optionIds"].Split('|');

            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the currency
            Currency currency = Currency.GetOneById(domain.currency);

            // Get the product by id
            Product product = Product.GetOneById(productId, domain.front_end_language);

            // Get translated texts
            KeyStringList tt = StaticText.GetAll(domain.front_end_language, "id", "ASC");

            // Make sure that the product not is null
            if (product == null)
            {
                return new EmptyResult();
            }

            // Get product values
            string productName = product.title;
            string productCode = product.product_code;
            string manufacturerCode = product.manufacturer_code;
            decimal unitPrice = product.unit_price;
            decimal unitFreight = product.unit_freight + product.toll_freight_addition;
            string variantImageUrl = product.variant_image_filename;

            // Update the added to cart statistic
            if (product.added_in_basket <= Int32.MaxValue - 1)
            {
                Product.UpdateAddedInBasket(product.id, product.added_in_basket + 1);
            }

            // The count of option ids
            Int32 optionIdCount = selectedOptionIds != null && selectedOptionIds[0] != "" ? selectedOptionIds.Length : 0;

            // Loop option identities and add to the price, freight and product code
            for (int i = 0; i < optionIdCount; i++)
            {
                // Convert the ids
                Int32 optionTypeId = Convert.ToInt32(productOptionTypeIds[i]);
                Int32 optionId = Convert.ToInt32(selectedOptionIds[i]);

                // Get the product option type and the product option
                ProductOptionType productOptionType = ProductOptionType.GetOneById(optionTypeId, domain.front_end_language);
                ProductOption productOption = ProductOption.GetOneById(optionTypeId, optionId, domain.front_end_language);

                // Add to values
                productName += "," + productOptionType.title + ": " + productOption.title;
                productCode += productOption.product_code_suffix;
                manufacturerCode += productOption.mpn_suffix;
                unitPrice += productOption.price_addition;
                unitFreight += productOption.freight_addition;
                variantImageUrl = variantImageUrl.Replace("[" + i + "]", productOption.product_code_suffix);
            }

            // Add delivery time to the product name
            productName += "," + tt.Get("delivery_time") + ": " + product.delivery_time;

            // Adjust the price and the freight with the conversion rate
            unitPrice *= (currency.currency_base / currency.conversion_rate);
            unitFreight *= (currency.currency_base / currency.conversion_rate);

            // Round the price to the minor unit for the currency
            Int32 decimalMultiplier = (Int32)Math.Pow(10, currency.decimals);
            unitPrice = Math.Round(unitPrice * (1 - product.discount) * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;
            unitFreight = Math.Round(unitFreight * decimalMultiplier, MidpointRounding.AwayFromZero) / decimalMultiplier;

            // Get the value added tax
            ValueAddedTax vat = ValueAddedTax.GetOneById(product.value_added_tax_id);

            // Create a cart item
            CartItem cartItem = new CartItem();
            cartItem.product_code = productCode;
            cartItem.product_id = product.id;
            cartItem.manufacturer_code = manufacturerCode;
            cartItem.product_name = productName;
            cartItem.quantity = quantity;
            cartItem.unit_price = unitPrice;
            cartItem.unit_freight = unitFreight;
            cartItem.vat_percent = vat.value;
            cartItem.variant_image_url = variantImageUrl;
            cartItem.use_local_images = product.use_local_images;

            // Add the cart item to the shopping cart
            CartItem.AddToShoppingCart(cartItem);

            // Check if prices should include VAT or not
            bool pricesIncludesVat = Session["PricesIncludesVat"] != null ? Convert.ToBoolean(Session["PricesIncludesVat"]) : domain.prices_includes_vat;

            // Get the current culture info
            CultureInfo cultureInfo = Tools.GetCultureInfo(Language.GetOneById(domain.front_end_language));

            // Get cart statistics
            Dictionary<string, decimal> cartStatistics = CartItem.GetCartStatistics(domain, pricesIncludesVat);

            // Create the dictionary to return
            Dictionary<string, string> cartData = new Dictionary<string, string>(3);
            cartData.Add("cart_quantity", cartStatistics["total_quantity"].ToString("##,0.##", cultureInfo));
            cartData.Add("cart_amount", cartStatistics["total_amount"].ToString("##,0.##", cultureInfo) + " " + domain.currency + (pricesIncludesVat == true ? " (" + tt.Get("including_vat").ToLower() + ")" : " (" + tt.Get("excluding_vat").ToLower() + ")"));
            cartData.Add("units_in_cart", tt.Get("units_in_cart").ToLower());

            // Return a dictionary with cart data
            return Json(cartData);

        } // End of the add_to_cart method

        // Send an email to the webmaster
        // POST: /home/add_product
        [HttpPost]
        public ActionResult contact_us(FormCollection collection)
        {
            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the form data
            string email = collection["txtEmail"];
            string subject = collection["txtSubject"] + " - " + domain.webshop_name;
            string message = collection["txtMessage"];

            // Modify the message
            message += "<p>";
            message = message.Replace(Environment.NewLine, "<br />");
            message += "</p>";

            // Get the signed in customer
            Customer customer = Customer.GetSignedInCustomer();

            // Send the message
            if(customer != null)
            {
                // Add a signature to the email
                message += "<hr>" + customer.contact_name + " | " + customer.id + "<br />" + customer.email;

                // Send the email
                Tools.SendEmailToCustomerService(email, subject, message);
            }
            else
            {
                Tools.SendEmailToHost(email, subject, message);
            }
            
            // Redirect the user to the start page
            return RedirectToAction("index", "home");

        } // End of the contact_us method

        // Sort categories on the index page
        // POST: /home/sort_category
        [HttpPost]
        public ActionResult sort_home(FormCollection collection)
        {
            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the form data
            string sort_field = collection["selectSortField"] != null ? collection["selectSortField"] : "id";
            string sort_order = collection["selectSortOrder"] != null ? collection["selectSortOrder"] : "ASC";
            string page_size = collection["selectPageSize"] != null ? collection["selectPageSize"] : "10";
            string display_view = collection["selectDisplayView"] != null ? collection["selectDisplayView"] : domain.default_display_view.ToString();

            // Create a new dictionary
            Dictionary<string, string> userSettings = new Dictionary<string, string>(4);
            userSettings.Add("sort_field", sort_field);
            userSettings.Add("sort_order", sort_order);
            userSettings.Add("page_size", page_size);
            userSettings.Add("display_view", display_view);

            // Save the dictionary to a session
            Session["UserSettings"] = userSettings;

            // Redirect the user to the start page
            return RedirectToAction("index", "home");

        } // End of the sort_home method

        // Sort categories and products in a category
        // POST: /home/sort_category
        [HttpPost]
        public ActionResult sort_category(FormCollection collection)
        {
            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the form data
            string page_name = collection["hiddenPageName"] != null ? collection["hiddenPageName"] : "";
            string sort_field = collection["selectSortField"] != null ? collection["selectSortField"] : "id";
            string sort_order = collection["selectSortOrder"] != null ? collection["selectSortOrder"] : "ASC";
            string page_size = collection["selectPageSize"] != null ? collection["selectPageSize"] : "10";
            string display_view = collection["selectDisplayView"] != null ? collection["selectDisplayView"] : domain.default_display_view.ToString();

            // Create a new dictionary
            Dictionary<string, string> userSettings = new Dictionary<string, string>(4);
            userSettings.Add("sort_field", sort_field);
            userSettings.Add("sort_order", sort_order);
            userSettings.Add("page_size", page_size);
            userSettings.Add("display_view", display_view);

            // Save the dictionary to a session
            Session["UserSettings"] = userSettings;

            // Redirect the user to the category page
            return RedirectToAction("category", "home", new { id = page_name });

        } // End of the sort_category method

        // Sort the search result
        // POST: /home/sort_search
        [HttpPost]
        public ActionResult sort_search(FormCollection collection)
        {
            // Get the current domain
            Domain domain = Tools.GetCurrentDomain();

            // Get the form data
            string keywords = collection["txtFormSearchBox"] != null ? collection["txtFormSearchBox"] : "";
            string sort_field = collection["selectSortField"] != null ? collection["selectSortField"] : "id";
            string sort_order = collection["selectSortOrder"] != null ? collection["selectSortOrder"] : "ASC";
            string page_size = collection["selectPageSize"] != null ? collection["selectPageSize"] : "10";
            string display_view = collection["selectDisplayView"] != null ? collection["selectDisplayView"] : domain.default_display_view.ToString();

            // Create a new dictionary
            Dictionary<string, string> userSettings = new Dictionary<string, string>(4);
            userSettings.Add("sort_field", sort_field);
            userSettings.Add("sort_order", sort_order);
            userSettings.Add("page_size", page_size);
            userSettings.Add("display_view", display_view);

            // Save the dictionary to a session
            Session["UserSettings"] = userSettings;
            
            // Redirect the user to the category page
            return RedirectToAction("search", "home", new { kw = Server.UrlEncode(keywords) });

        } // End of the sort_search method

        #endregion

        #region Helper methods

        // Set the layout of the page
        // GET: /home/layout/mobile
        [HttpGet]
        public ActionResult layout(string id = "")
        {
            // Create a new cookie
            HttpCookie aCookie = new HttpCookie("LayoutType");
            aCookie.Value = id;

            // Get webshop settings
            KeyStringList webshopSettings = WebshopSetting.GetAllFromCache();
            string redirectHttps = webshopSettings.Get("REDIRECT-HTTPS");

            // Set the expiration and add the cookie
            aCookie.Expires = DateTime.UtcNow.AddDays(1);
            aCookie.HttpOnly = true;
            aCookie.Secure = redirectHttps.ToLower() == "true" ? true : false;
            Response.Cookies.Add(aCookie);

            // Redirect the user to the new url
            return Redirect("/");

        } // End of the layout method

        // Change the display prices to include vat or not to include vat
        // GET: /home/prices_includes_vat/true
        [HttpGet]
        public ActionResult prices_includes_vat(string id = "false", string url = "/")
        {
            // Change the vat setting
            Session["PricesIncludesVat"] = id;

            // Redirect the user to the page that he was on
            return Redirect(url);

        } // End of the prices_includes_vat method

        #endregion

    } // End of the class

} // End of the namespace