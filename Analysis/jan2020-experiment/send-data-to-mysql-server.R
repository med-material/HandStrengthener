library(stringr)
library(gsheet)
library(dplyr)
D = readbulk::read_bulk('jan2020-bcidata', sep=',', na.strings = 'NA', stringsAsFactors=FALSE)
D = subset(D, select = -c(X,X.1,X.2,X.3,X.4,X.5,X.6) ) # Remove the X columns which appear because of whitespaces in the CSV files.
D$PID <- str_match(D$File, "(participant)+(\\d+)")[,3] # get PID from filenames with RegEx and insert as column into our dataframe

condition_url <- 'https://docs.google.com/spreadsheets/d/1yZvteDBjv8wWX7xrHwRqDHbimZOUhahzvQfvMsLs_5U/edit#gid=2134951077'
survey.conditiondata <- gsheet2tbl(condition_url)

experiment_url <- 'https://docs.google.com/spreadsheets/d/1QAUIH91jffkEhVtnHzUGtxR3NdBwYqNRMrx_Zf9Z9Pg/edit#gid=434757515'
survey.experimentdata <- gsheet2tbl(experiment_url)

#detect faulty column
# na.omit(D$Date.Timestamp.Event.KeyCode.SequenceTime_ms.TimeSinceLastKey_ms.KeyOrder.KeyType.ExpectedKey1.ExpectedKey2.SequenceNumber.SequenceComposition.SequenceSpeed.SequenceValidity.SequenceType.SequenceWindowClosure.TargetFabInputRate.TargetRecognitionRate.StartPolicyReview.Trials.InterTrialIntervalSeconds.InputWindowSeconds.GameState.GamePolicy.CurrentRecognitionRate.FabAlarmFixationPoint.FabAlarmVariability.CurrentFabRate.CurrentFabAlarm.PID.Age.Gender.Name.ConditionOrder)

# Fix PID 103s number to be 600 instead to not confuse him with others.
D$PID[D$PID == 103] <- 600

newsettingsPID = c(602,603,604) # PIDs which did have prior training
oldsettingsPID = c(101, 600, 108, 206, 210, 402, 404, 601, 302, 303) #PIDs which had no prior training


# Add missing values we were not logging from Unity (Deadzone and sequenceWindowTimeLimit).
# SequenceDeadzone: apply 0.75 to newsettingsPIDs and 0.4 to oldsettingsPIDs
# SequenceWindowTimeLimit: apply 1.5 to newsettingsPIDs and 1.3 to oldsettingsPIDs
D$SequenceDeadzone = NA
D$SequenceWindowTimeLimit = NA
D = D %>% mutate(SequenceDeadzone = ifelse(PID %in% newsettingsPID, 0.75, 0.4))
D = D %>% mutate(SequenceWindowTimeLimit = ifelse(PID %in% newsettingsPID, 1.5, 1.3))

# Add participant ages
D$Age = NA
D$Age[D$PID == 101] <- 25
D$Age[D$PID == 602] <- 23
D$Age[D$PID == 604] <- 24
D$Age[D$PID == 603] <- 22
D$Age[D$PID == 210] <- 26
D$Age[D$PID == 601] <- 30
D$Age[D$PID == 404] <- 23
D$Age[D$PID == 108] <- 30
D$Age[D$PID == 402] <- 30
D$Age[D$PID == 206] <- 23
D$Age[D$PID == 600] <- 25
D$Age[D$PID == 302] <- 22
D$Age[D$PID == 303] <- 26


# Check if we are missing any: D[is.na(D$Age),]

# Add participant genders
D$Gender = NA
D$Gender[D$PID == 101] <- 'M'
D$Gender[D$PID == 602] <- 'F'
D$Gender[D$PID == 604] <- 'M'
D$Gender[D$PID == 603] <- 'M'
D$Gender[D$PID == 210] <- 'M'
D$Gender[D$PID == 601] <- 'M'
D$Gender[D$PID == 404] <- 'F'
D$Gender[D$PID == 108] <- 'M'
D$Gender[D$PID == 402] <- 'M'
D$Gender[D$PID == 206] <- 'M'
D$Gender[D$PID == 600] <- 'M'
D$Gender[D$PID == 302] <- 'M'
D$Gender[D$PID == 303] <- 'M'

#Add participant motoric impairments
D$MotoricImpairment = FALSE
D$MotoricImpairment[D$PID == 602] <- TRUE # TODO: Check with Yohann if this is correct.
D$Gender[D$PID == 600] <- NA

#Add procedure version
D$ProcedureVersion = NA # What version of the procedure the participant received ("v1", "v2")
D = D %>% mutate(ProcedureVersion = ifelse(PID %in% newsettingsPID, "v1", "v0.9"))


# To Replace 'NA' with NA
# D[D=='NA'] <- NA

# Fix timestamps and dates
# Doing Date/Timestamp individually does not work well, so lets just stick to a single DateTime column.
#D$Date <- as.POSIXct(D$Date, format = "%Y-%m-%d")
#D$Timestamp <- as.POSIXct(D$Timestamp, format = "%H:%M:%OS")

# Convert the Timestamps to DateTime including the milliseconds.
# WARNING: If you did "Save" in Excel after importing, the timestamp format may be different.
D$DateTime <- paste(D$Date,D$Timestamp)
# WARNING: Converting the timestamp to POSIXct will cause dbWriteTable for remove all milliseconds.
# For the time being it is better to keep it as a string.
#D$DateTime <- as.POSIXct(D$DateTime, format = "%Y-%m-%d %H:%M:%OS")
D = subset(D, select = -c(Date,Timestamp )) # Remove the old date columns.
#to see the 4 decimals in R: format(D$DateTime, "%Y-%m-%d %H:%M:%OS4")
format(D$DateTime, "%Y-%m-%d %H:%M:%OS4")

# Add conditions
D$Condition[D$TargetFabInputRate == 0] <- 'B'
D$Condition[D$TargetFabInputRate == 0.1] <- 'D'
D$Condition[D$TargetFabInputRate == 0.2] <- 'A'
D$Condition[D$TargetFabInputRate == 0.3] <- 'C'
D$Condition[D$TargetFabInputRate == 0.4] <- 'E' # some of our participants accidentically got 0.4 fab input

# Convert unconverted columns to numeric values
# debug with str(D)
D$PID <- as.numeric(D$PID)
D$SequenceTime_ms[D$SequenceTime_ms=='NA'] <- NA
D$SequenceTime_ms <- as.numeric(D$SequenceTime_ms)
D$CurrentFabAlarm[D$CurrentFabAlarm=='NA'] <- NA
D$CurrentFabAlarm <- as.numeric(D$CurrentFabAlarm)

# Note that for 302 we are missing the data, but I have created a "missing" file with 1 row, just so his condition is represented and we can merge with survey data.
#D <- D %>% left_join(survey.conditiondata, by=c("PID","Condition"))
#D <- D %>% left_join(survey.experimentdata, by=c("PID"))


# Convert timestamps in survey.
D$Surv.Cond.Timestamp <- as.POSIXct(D$Surv.Cond.Timestamp, format = "%d/%m/%Y %H:%M:%S")
D$Surv.Exp.Timestamp <- as.POSIXct(D$Surv.Exp.Timestamp, format = "%d/%m/%Y %H:%M:%S")

# Rename columns
D %>% 
  rename(
    Event = Kb.KeyCode,
    sepal_width = Sepal.Width
  )

E = data.frame(DateTime = D$DateTime, 
               Event = D$Event, 
               PID = D$PID, 
               TestCondition = D$Condition, 
               Kb.KeyCode = D$KeyCode, 
               Kb.KeyValid = D$KeyType, 
               Kb.ExpectedKey1 = D$ExpectedKey1, 
               Kb.ExpectedKey2 = D$ExpectedKey2, 
               Kb.KeyOrder = D$KeyOrder, 
               Kb.TimeSinceLastKeySeconds = D$TimeSinceLastKey_ms, 
               Kb.SequenceComposition = D$SequenceComposition,
               Kb.SequenceNumber = D$SequenceNumber,
               Kb.SequenceSpeed = D$SequenceSpeed, 
               Kb.SequenceType = D$SequenceType, 
               Kb.SequenceValidity = D$SequenceValidity, 
               Kb.SequenceWindowClosure = D$SequenceWindowClosure, 
               Kb.SequenceDeadzone = D$SequenceDeadzone, 
               Kb.SequenceSpeedTarget = D$SequenceWindowTimeLimit,
               InputWindow.DurationSeconds = D$InputWindowSeconds,
               InterTrial.DurationSeconds = D$InterTrialIntervalSeconds,
               RealInput.TargetRate = D$TargetRecognitionRate,
               FabInput.TargetRate = D$TargetFabInputRate,
               FabInput.FixationPointSeconds = D$FabAlarmFixationPoint,
               FabInput.VariabilitySeconds = D$FabAlarmVariability,
               FabInput.CurrentFixationPoint = D$CurrentFabAlarm,
               Trials.Amount = D$Trials,
               RealInput.CurrentRate = D$CurrentRecognitionRate,
               FabInput.CurrentRate = D$CurrentFabRate,
               GamePolicy.Type = D$GamePolicy,
               GamePolicy.ReviewOnTrial = D$StartPolicyReview,
               GameState = D$GameState, 
               Exp.GameVersion = "v2020.01.13")

# Debugging that conditions look correct in the survey data.
#options(max.print=6000)
#D[D$PID == 604, c("Date","Timestamp", "Condition", "DateTime","TargetFabInputRate")]

# Upload to a database (TODO)
library(RMySQL)
cred <- read.csv("credentials.csv", header=TRUE,sep=",", colClasses=c("character","character","character","character"))
mydb = dbConnect(MySQL(), user=cred[1, "username"], password=cred[1, "password"], dbname=cred[1, "dbname"], host=cred[1, "host"])
dbCreateTable(mydb, "handdata_jan2020",D)
#D_sorted <- D[order(D$DateTime),]
dbWriteTable(mydb, "HandStrengthJan20Experiment", E, append = TRUE, 
             field.types = c(DateTime="datetime(6)", 
                             Event="varchar(50)", 
                             PID="int",
                             TestCondition = "varchar(50)", 
                             Kb.KeyCode = "varchar(50)", 
                             Kb.KeyValid = "varchar(50)", 
                             Kb.ExpectedKey1 = "varchar(50)", 
                             Kb.ExpectedKey2 = "varchar(50)", 
                             Kb.KeyOrder = "int", 
                             Kb.TimeSinceLastKeySeconds = "double", 
                             Kb.SequenceComposition = "varchar(50)",
                             Kb.SequenceNumber = "int",
                             Kb.SequenceSpeed = "varchar(50)", 
                             Kb.SequenceType = "varchar(50)", 
                             Kb.SequenceValidity = "varchar(50)", 
                             Kb.SequenceWindowClosure = "varchar(50)", 
                             Kb.SequenceDeadzone = "double", 
                             Kb.SequenceSpeedTarget = "double",
                             InputWindow.DurationSeconds = "double",
                             InterTrial.DurationSeconds = "double",
                             RealInput.TargetRate = "double",
                             FabInput.TargetRate = "double",
                             FabInput.FixationPointSeconds = "double",
                             FabInput.VariabilitySeconds = "double",
                             FabInput.CurrentFixationPoint = "double",
                             Trials.Amount = "int",
                             RealInput.CurrentRate = "double",
                             FabInput.CurrentRate = "double",
                             GamePolicy.Type = "varchar(50)",
                             GamePolicy.ReviewOnTrial = "varchar(50)",
                             GameState = "varchar(50)", 
                             Exp.GameVersion = "varchar(50)"), row.names=FALSE)  # send data to database



