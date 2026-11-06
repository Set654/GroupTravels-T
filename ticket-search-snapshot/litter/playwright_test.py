# from playwright.sync_api import sync_playwright

# def main():
#     with sync_playwright() as p: #менеджер контекста
#         browser = p.chromium.launch(headless = False) # True/False отечает за то чтобы браузер открывался в скрытом или не скрытом режиме

#         page = browser.new_page() 
#         page.goto("https://sandbox.oxylabs.io/products")

#         page.wait_for_selector(".product-card") #Автоматически ждет пока product card загрузиотся
#         products = page.query_selector_all(".product-card")

#         print("=" * 50)
#         print("Extracted products")
#         print("=" * 50)

#         for product in products:
#             name_element = product.query_selector("h4")
#             name = name_element.inner_text() if name_element else '-'

#             price_element = product.query_selector(".price-wrapper")
#             price = price_element.inner_text().strip() if price_element else '-'
 
#             print(f"Product: {name}")
#             print(f"Price: {price}")
#             print("-" * 30)


#         print("\n" + "=" * 50)
#         print("Using playwright  locators")
#         print("-" * 30)

#         all_links = page.get_by_role("link").all()
#         print(f"Found {len(all_links)} links on the page")

#         heading = page.get_by_text("Products").first
#         print(f"Found heading: {heading.inner_text()}" )


#         browser.close()
# if __name__ == "__main__":
#     main()

# from playwright.sync_api import sync_playwright

# def main():
#     with sync_playwright() as p:
#         browser = p.chromium.launch(headless= False)
#         page = browser.new_page()
#         page.goto("https://sandbox.oxylabs.io/products")

#         page.locator(".product-card")
#         pagination = page.locator(".pagination")

#         if pagination.is_visible():
#             page_links = page.locator(".pagination a").all()
#             print(f"Found {len(page_links)} pages")

#             next_button = page.locator("text=Forward").first

#             if next_button.is_visible():
#                 print("clicking 'next' button")
#                 next_button.click()

#                 page.locator(".product-card").first.wait_for(state = "visible")

#                 products = page.locator(".product-card").all()
#                 print(f"Found {len(products)} products on page 2")

#         browser.close()
# if __name__ == '__main__':
#     main()

from playwright.sync_api import sync_playwright

def main():
        with sync_playwright() as p:
            browser = p.chromium.launch(headless = True)
            page = browser.new_page()
            page.goto("https://sandbox.oxylabs.io/products")

            page.locator(".product-card").first.wait_for(state = "visible")
            cards = page.locator(".product-card").all()

            print(f"Найдено товаров на странице: {len(cards)}\n")
            for card in cards:
                  title_locator = card.locator("h4")
                  title = title_locator.inner_text().strip()

                  price_locator = card.locator(".price-wrapper")
                  price = price_locator.inner_text().strip()

                  print(f"{title} - {price}")

            browser.close()

if __name__ == "__main__":
      main()