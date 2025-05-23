# NopCommerce 4.5

## Installation Steps
1. Clone the repository : git clone https://github.com/Ojha7447/Nop45.git
2. Install dependencies (if any).
3. Provide a connection string
   
## 🔧 Features

- Custom API plugin with JWT authentication
- Example plugin: `Nop.Plugin.Misc.Api`
- Custom Discount Plugin : Nop.Plugin.Misc.CustomDiscount

## 📦 Requirements

- Visual Studio 2019 or newer
- .NET Core SDK 6.0+
- SQL Server (any edition)
- NopCommerce 4.5 source code

## How to Configure the Custom Discount Plugin

1. **Start the application.**

2. **Install the plugin** `Nop.Plugin.Misc.CustomDiscount` from the admin panel:
   - Go to **Admin > Configuration > Local plugins**
   - Find `Nop.Plugin.Misc.CustomDiscount` in the list
   - Click **Install** and then **Restart Application** if prompted

3. **Configure the Discount:**
   - Navigate to **Admin > Promotions > Discounts**
   - Click **Add new** to create a new discount
   - Fill in the required discount information such as:
     - Name
     - Discount type ('Assigned to order subtotal')
     - Discount amount as percantage (e.g 10%)
     - Start/End date (optional)
   - Under **Requirements**, click **Add requirement group**
   - Select **Assigned on customer orders** from the dropdown (this is registered by the plugin)
   - Configure the custom values as per your business rules
   - Click **Save**

4. **Test the Discount:**
   - Go to the public store
   - Add eligible products to the cart
   - Ensure the discount is applied correctly according to the conditions

## How to Test the API Endpoint
1. Start the application.
2. Install the plugin `Nop.Plugin.Misc.Api` from the admin panel:
   - Go to **Admin > Configuration > Local plugins**
   - Find `Nop.Plugin.Misc.Api` and click **Install**
3. Use the provided curl commands to test the API endpoints:
   - You can run these curl commands directly in a terminal
   - Or import them into Postman for easier testing
   - Alternatively, use the Swagger UI at `https://localhost:44369/swagger` to interact with the API
> The curl commands are attached in the email for your convenience.

## How to Add a "Gift Message" Field During Checkout in NopCommerce
    Go to **Admin > Catalog > Attributes > Checkout Attributes**.
    Click **Add new**.
    Set the Name to "Gift Message".
    Choose Control Type as Multiline Textbox (or another type as needed).
    Click Save.
    
How to test it:
    On the public store’s Cart page, you will see the Gift Message field below the list of items.
    Enter your message and proceed to checkout.
    On the Confirm Order page, the gift message will be displayed above the order summary.
    
    After placing the order, the message will be visible on:
        The Customer’s Order Details page (above the order summary).
        The Admin panel under the Order Details page, in the products section below the products table.





