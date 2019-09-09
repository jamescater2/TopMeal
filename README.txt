
TopMeal Web API calls
=====================

POST api/auth/register [UserForRegisterDto]
POST api/auth/login [UserForLoginDto]

GET api/meal
GET api/meal/{filter}
GET api/meal/user/{userId}
GET api/meal/user/{userId}/{filter}
GET api/meal/remaining
GET api/meal/remaining/{userId}
GET api/meal/help
POST api/meal/{description}
POST api/meal/{calories}
POST api/meal/{calories}/{description}
POST api/meal [Meal]
PUT api/meal/{id}/{calories}
PUT api/meal/{id} [Meal]
DELETE api/meal/{id}
DELETE api/meal/alluser/{id}

GET api/user
GET api/user/{filter}
GET api/user/{id}
GET api/user/{id}/{filter}
GET api/user/help
POST api/user [User]\n" +
PUT api/user/{id} [User]\n" +
DELETE api/user/{id}

GET api/role
GET api/role/{id}
GET api/role/{filter}
GET api/role/help
POST api/role/{name}
POST api/user [Role]
PUT api/role/{id} [Role]
DELETE api/user/{id}
