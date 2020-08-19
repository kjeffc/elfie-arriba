# Steps to run docker-localy

## Generate CSEFY19 Personal Access Token
- Navigate here: https://dev.azure.com/csefy19/_usersSettings/tokens
- Create a New Token
- Select the following scopes
    
        Analytics(Read) Work Items(Read)

## Run docker-compose locally
- Set the minimial environment variables
        
        export AZPAT={Copied Token Value}
- Ensure you are on the cse-config and have the latest changes
  - Check that there are no edits in progress. If there are please save them

        git status
  - Checkout cse-config
  
        git checkout cse-config

  - Follow these steps if the branch doesn't exist in your fork
  
        git remote add eric https://github.com/ericmaino/elfie-arriba.git
        git fetch -ap eric
        git checkout -b cse-config
        git reset --hard eric/cse-config
        
- Run docker-compose to build and start

        docker-compose up --build

- Once running navigate to the local page

        http://localhost:8080

