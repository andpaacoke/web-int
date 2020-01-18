from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
import re

def readFile():
    file = open("SentimentTrainingData.txt", "r")
    lines = file.readlines()
    file.close()
    reviewDictionary = {}
    sentimentClass = 0
    index = 0
    for line in lines:
        if line.startswith("review/score"):
            line = line.replace("review/score:", "")
            line = re.sub("<[^>]*>", "", line)
            line = line.replace("\n", "")
            if int(float(line)) < 3:
                sentimentClass = 0
            else:
                sentimentClass = 1    
        if line.startswith("review/text"):
            line = line.replace("review/text:", "")
            line = re.sub("<[^>]*>", " ", line).lower()
            reviewDictionary[index] = [sentimentClass, line]
            index = index + 1

    return reviewDictionary
    

"""     scores = []
    for line in reviews:
        scores.append(str(analyzer.polarity_scores(line).get('compound')))

    print("så skrives der sgu til filen")
    file = open("scores.txt", "w")
    for score in scores:
        file.write(score + "\n")

 """

def giveSentimentsToTrainingData():
    reviewDictionary = readFile()
    
    totalNumberOfReviews = len(reviewDictionary)

    positiveReviews = {k:v for (k,v) in reviewDictionary.items() if v[0] == 1}
    negativeReviews = {k:v for (k,v) in reviewDictionary.items() if v[0] == 0}

    numberOfPositiveReviews = len(positiveReviews)
    numberOfNegativeReviews = len(negativeReviews)


    propabilityOfPositive = (numberOfPositiveReviews + 1) / (totalNumberOfReviews + 2)
    propabilityOfNegative = (numberOfNegativeReviews + 1) / (totalNumberOfReviews + 2)

    positiveWordCount = {}
    negativeWordCount = {}
    allReviewsWordCount = {}

    for k,v in positiveReviews.items():
        v[1] = v[1].strip()
        reviewSplit = v[1].split(" ")
        for word in reviewSplit:
            if word in positiveWordCount:
                positiveWordCount[word] = positiveWordCount[word] + 1
            else:
                positiveWordCount[word] = 1
            if word in allReviewsWordCount:
                allReviewsWordCount[word] = allReviewsWordCount[word] + 1
            else:
                allReviewsWordCount[word] = 1

    for k, v in negativeReviews.items():
        v[1] = v[1].strip()
        reviewSplit = v[1].split(" ")
        for word in reviewSplit:
            if word in negativeWordCount:
                negativeWordCount[word] = negativeWordCount[word] + 1
            else:
                negativeWordCount[word] = 1
            if word in allReviewsWordCount:
                allReviewsWordCount[word] = allReviewsWordCount[word] + 1
            else:
                allReviewsWordCount[word] = 1

    totalNumberOfWordsNegative = 0
    for k, v in negativeWordCount.items():
        totalNumberOfWordsNegative = totalNumberOfWordsNegative + negativeWordCount[k]

    totalNumberOfWordsPositive = 0
    for k, v in positiveWordCount.items():
        totalNumberOfWordsPositive = totalNumberOfWordsPositive + positiveWordCount[k]

    totalNumberOfWordsAll= 0
    for k, v in allReviewsWordCount.items():
        totalNumberOfWordsAll = totalNumberOfWordsAll + allReviewsWordCount[k]

    conditionalProbabilityNegativeReviews = {}
    conditionalProbabilityPositiveReviews = {}

    for k, v in negativeWordCount.items():
        conditionalProbabilityNegativeReviews[k] = (v + 1) / (totalNumberOfWordsNegative + len(allReviewsWordCount))

    for k, v in positiveWordCount.items():
        conditionalProbabilityPositiveReviews[k] = (v + 1) / (totalNumberOfWordsPositive + len(allReviewsWordCount))
    
    count = 0
    for key in conditionalProbabilityPositiveReviews:
        if count < 5:
            print(key)
            print(conditionalProbabilityPositiveReviews[key])
            print(len(conditionalProbabilityPositiveReviews))
        count += 1


    predictions = {}
    for k, v in reviewDictionary.items():
        positiveScore = propabilityOfPositive
        negativeScore = propabilityOfNegative
        v[1] = v[1].strip()
        reviewSplit = v[1].split(" ")
        for word in reviewSplit:
            if word in conditionalProbabilityPositiveReviews.keys():
                positiveScore = positiveScore * conditionalProbabilityPositiveReviews[word]
            else:
                positiveScore = positiveScore * 0
            if word in conditionalProbabilityNegativeReviews.keys():
                negativeScore = negativeScore * conditionalProbabilityNegativeReviews[word]
            else:
                negativeScore = negativeScore * 0
        if (positiveScore > negativeScore):
            predictions[k] = 'Positive'
        else:
            predictions[k] = 'Negative'
    print(predictions.values())
        

    

giveSentimentsToTrainingData()

