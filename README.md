# Azure Function samples with MongoDB Atlas

Use the following JSON to create a sample restaurant:

  ```json
  { 
    "address" : {
        "building" : "2780", 
        "coord" : [
            -73.98241999999999, 
            40.579505
        ], 
        "street" : "Stillwell Avenue", 
        "zipcode" : "11224"
    }, 
    "borough" : "Brooklyn", 
    "cuisine" : "American", 
    "name" : "Riviera Caterer", 
    "restaurant_id" : "40356018"
}
  ```

  or the following cURL command:

  ```
	  curl -X POST \
	  http://localhost:7071/api/CreateRestaurant \
	  -H 'cache-control: no-cache' \
	  -H 'content-type: application/json' \
	  -d '{ 
		"address" : {
			"building" : "2780", 
			"coord" : [
				-73.98241999999999, 
				40.579505
			], 
			"street" : "Stillwell Avenue", 
			"zipcode" : "11224"
		}, 
		"borough" : "Brooklyn", 
		"cuisine" : "American", 
		"name" : "Riviera Caterer", 
		"restaurant_id" : "40356018"
	}'
  ```