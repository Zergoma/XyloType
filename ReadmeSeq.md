< Go back to [main readme](Readme.md)  

# 📊 Seq

Seq is used locally to visualize and query the application's structured logs.

## 🚀 Start Seq

Make sure Docker is running, then start the Seq container:

>docker compose up -d


Check that the container is running:

>docker compose ps  

🌐 Open Seq

Once the container is running, open:

http://localhost:5341

Seq is exposed locally on port `5341`.

### 🔐 First connection

On the first connection, use the administrator credentials defined in the local .env file:

SEQ_PASSWORD=your_password

	

Username	admin  
Password	Value of SEQ_PASSWORD  

`⚠️ .env contains local credentials and must not be committed to Git.`

## 🛑 Stop Seq

To stop the Seq container:

>docker compose down

The Seq data is stored in a Docker volume, so stopping the container does not remove the stored logs.

## 🗑️ Remove Seq data

To remove the container and all stored Seq data:

>docker compose down -v

`⚠️ This permanently removes the Seq Docker volume and all logs stored in it.`