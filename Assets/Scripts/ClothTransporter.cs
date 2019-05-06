using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothTransporter : MonoBehaviour {
    private Cloth listOfCloth;
    public GameObject receiver;

    // Use this for initialization
    void Start() {
        listOfCloth = (Cloth)GetComponents(typeof(Cloth))[0];
    }

    // Update is called once per frame
    void Update() {
        if (listOfCloth != null && receiver != null) {
            foreach (LayerDynamic ma in receiver.GetComponents(typeof(LayerDynamic))) {
                ma.sendVerticles(listOfCloth.vertices);
            }
        }
    }
}
