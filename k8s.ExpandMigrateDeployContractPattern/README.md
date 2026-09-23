

## Run Postgres in docker for local testing
![alt text](image-1.png)

## Initial Local 
- Create the Migrations
- Update the database

![alt text](image.png)


![alt text](image-2.png)

![alt text](image-3.png)

![alt text](image-4.png)

- Run the actual deployment to create the Order Api
![alt text](image-5.png)

![alt text](image-7.png)

Now change the Order Entity to have Priority which is "Allow Null" column.
- Run the Migration and run update database for local postgres testing
![alt text](image-6.png)

Create the v2 version of docker image and push to docker hub.

## Build the V2 and push to docker hub
![alt text](image-8.png) 

- Create the migration-v2.expand.yml job and run it.
![alt text](image-9.png)

Test the New columns added to the database table

![alt text](image-10.png)

![alt text](image-11.png)

![alt text](image-12.png)

![alt text](image-13.png)

Now update the null value IsPriority column to false using some query.
![alt text](image-14.png)

## Now deploy v2 and check the roll-out status
![alt text](image-15.png)

## Now change the Entity IsPriority as Not Null and create Migration
![alt text](image-16.png)

## Build and Push the version 3 image to Docker hub
![alt text](image-17.png)

![alt text](image-18.png)

## Check the database now for the column to get updated to Not Null

![alt text](image-20.png)

![alt text](image-19.png)

![alt text](image-21.png)

![alt text](image-22.png)
